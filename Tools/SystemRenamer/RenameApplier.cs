using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SystemRenamer;

/// <summary>Outcome of applying renames.</summary>
public sealed record ApplyResult(
    int DocumentsWritten,
    int FilesRenamed,
    List<string> Failures,
    List<string> GitMvFailures,
    Dictionary<string, int> EditsPerAssembly);

/// <summary>
/// Finds all references to every target, rewrites identifiers, and renames files using
/// <c>git mv</c> so the history is preserved as a rename rather than a delete+create.
/// </summary>
public sealed class RenameApplier
{
    private readonly Solution _solution;
    private readonly List<RenameTarget> _targets;
    private readonly string _root;
    private readonly string _engineRoot;
    private readonly bool _noGit;
    private readonly bool _debugChanges;

    public RenameApplier(Solution solution, List<RenameTarget> targets, string root, bool noGit, bool debugChanges = false)
    {
        _solution = solution;
        _targets = targets;
        _root = Path.GetFullPath(root);
        _engineRoot = Path.Combine(_root, "RobustToolbox");
        _noGit = noGit;
        _debugChanges = debugChanges;
    }

    public async Task<ApplyResult> ApplyAsync()
    {
        var edits = new ConcurrentDictionary<DocumentId, ConcurrentBag<TextChange>>();
        var renames = new ConcurrentDictionary<DocumentId, (string OldName, string NewName)>();

        // Build target lookup by metadata identity (assembly + namespace + name). This lets us
        // disambiguate the same simple name (e.g. "AccessSystem") across shared/server/client.
        var targetKeys = new Dictionary<(string Asm, string Ns, string Name), RenameTarget>(_targets.Count);
        var oldNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in _targets)
        {
            targetKeys[(t.Symbol.ContainingAssembly?.Name ?? t.AssemblyName, t.TypeNamespace, t.Symbol.Name)] = t;
            oldNames.Add(t.OldName);
        }

        // Comment/string cleanup regex for shared-system names (plain comments are not reported
        // by symbol binding, e.g. "// TODO: use SharedFooSystem"). Applied to the in-memory content
        // of every rewritten document so renamed files get it too.
        var (sharedMap, sharedRegex) = BuildSharedCommentRegex();

        // Declarations (also drives file renames).
        foreach (var target in _targets)
        {
            foreach (var declRef in target.Symbol.DeclaringSyntaxReferences)
            {
                var doc = _solution.GetDocument(declRef.SyntaxTree);
                if (doc is null)
                    continue;

                var cls = declRef.GetSyntax() as ClassDeclarationSyntax;
                if (cls is null)
                    continue;

                var change = new TextChange(cls.Identifier.Span, target.NewName);
                edits.AddOrUpdate(doc.Id, _ => new ConcurrentBag<TextChange> { change },
                    (_, bag) => { bag.Add(change); return bag; });
                renames[doc.Id] = (target.OldName, target.NewName);
            }
        }

        // Reference scan: bind every simple name whose text matches an old target name.
        // More reliable than SymbolFinder.FindReferencesAsync, which can miss references in
        // qualified names such as "Server.Atmos.EntitySystems.AtmosphereSystem".
        Console.WriteLine($"Scanning references for {_targets.Count} targets...");
        var docs = _solution.Projects.SelectMany(p => p.Documents).ToList();
        var scanned = 0;
        await Parallel.ForEachAsync(docs, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(4, Environment.ProcessorCount / 2) },
            async (doc, ct) =>
            {
                var root = await doc.GetSyntaxRootAsync(ct);
                var model = await doc.GetSemanticModelAsync(ct);
                if (root is null || model is null)
                    return;

                foreach (var node in root.DescendantNodes())
                {
                    if (node is not SimpleNameSyntax sn)
                        continue;
                    if (!oldNames.Contains(sn.Identifier.ValueText))
                        continue;

                    var symbol = ResolveBoundSymbol(model, sn);
                    var key = TypeKeyOf(symbol);
                    if (key is null || !targetKeys.TryGetValue(key.Value, out var target))
                        continue;

                    if (_debugChanges)
                    {
                        var msg = $"  SCAN {doc.Name}@{sn.Identifier.Span}: '{sn.Identifier.ValueText}' -> {symbol!.ToDisplayString()} [key={key.Value}] => {target.OldName}->{target.NewName}";
                        Console.WriteLine(msg);
                        try
                        {
                            File.AppendAllText(Path.Combine(_root, "renamer-changes.log"), msg + "\n");
                        }
                        catch
                        {
                            // best-effort logging
                        }
                    }

                    var change = new TextChange(sn.Identifier.Span, target.NewName);
                    edits.AddOrUpdate(doc.Id, _ => new ConcurrentBag<TextChange> { change },
                        (_, bag) => { bag.Add(change); return bag; });
                }

                if (Interlocked.Increment(ref scanned) % 2000 == 0)
                    Console.WriteLine($"  scanned {scanned}/{docs.Count} documents");
            });

        Console.WriteLine("Applying edits...");
        var written = 0;
        var renamed = 0;
        var failures = new List<string>();
        var gitFailures = new List<string>();
        var perAssembly = new Dictionary<string, int>();

        foreach (var project in _solution.Projects)
        {
            foreach (var doc in project.Documents)
            {
                if (!edits.TryGetValue(doc.Id, out var bag))
                    continue;

                var path = doc.FilePath;
                if (path is null)
                {
                    failures.Add($"Document {doc.Name} has no file path; skipped.");
                    continue;
                }

                perAssembly.TryGetValue(project.AssemblyName, out var count);
                perAssembly[project.AssemblyName] = count + 1;

                // Sort + dedupe the changes.
                var changes = bag
                    .GroupBy(c => (c.Span.Start, c.Span.Length, c.NewText))
                    .Select(g => g.First())
                    .OrderBy(c => c.Span.Start)
                    .ToList();

                if (_debugChanges)
                {
                    var renameNote = renames.TryGetValue(doc.Id, out var rd) ? $"{rd.OldName}->{rd.NewName}" : "content only";
                    var msg = $"\n== {path} ({renameNote})";
                    foreach (var c in changes)
                        msg += $"\n    [{c.Span.Start},{c.Span.Length}] -> '{c.NewText}'";
                    Console.WriteLine(msg);
                    try
                    {
                        File.AppendAllText(Path.Combine(_root, "renamer-changes.log"), msg + "\n");
                    }
                    catch
                    {
                        // best-effort logging
                    }
                }

                var text = await doc.GetTextAsync();
                string newContent;
                try
                {
                    // All spans were computed against the original text; pass them all at once
                    // so Roslyn applies them correctly regardless of length changes.
                    newContent = text.WithChanges(changes).ToString();
                    if (sharedRegex is not null)
                        newContent = sharedRegex.Replace(newContent, m => sharedMap![m.Value]);
                }
                catch (Exception e)
                {
                    failures.Add($"{path}: failed to apply changes ({e.Message})");
                    continue;
                }

                WriteFile(path, newContent);
                written++;

                if (renames.TryGetValue(doc.Id, out var rn))
                {
                    var newPath = ComputeNewPath(path, rn.OldName, rn.NewName);
                    if (newPath != path)
                    {
                        if (GitMv(path, newPath, out var err))
                            renamed++;
                        else
                            gitFailures.Add($"{path} -> {newPath}: {err}");
                    }
                }
            }
        }

        // Comment/string cleanup for documents that had NO symbol edits (a comment may reference
        // a renamed system without the code using it). Renamed files were already handled
        // in-memory before git mv; these files keep their original paths.
        Console.WriteLine("Comment/string cleanup pass...");
        if (sharedRegex is not null)
        {
            foreach (var project in _solution.Projects)
            {
                foreach (var doc in project.Documents)
                {
                    var path = doc.FilePath;
                    if (path is null || !File.Exists(path))
                        continue;
                    var content = File.ReadAllText(path);
                    if (!content.Contains("Shared"))
                        continue;
                    var updated = sharedRegex.Replace(content, m => sharedMap![m.Value]);
                    if (updated != content)
                    {
                        WriteFile(path, updated);
                        written++;
                    }
                }
            }
        }

        return new ApplyResult(written, renamed, failures, gitFailures, perAssembly);
    }

    private (Dictionary<string, string>? Map, Regex? Regex) BuildSharedCommentRegex()
    {
        var sharedPairs = _targets
            .Where(t => t.Kind == "shared")
            .Select(t => (t.OldName, t.NewName))
            .GroupBy(t => t.OldName)
            .Select(g => g.First())
            .ToList();

        if (sharedPairs.Count == 0)
            return (null, null);

        var map = sharedPairs.ToDictionary(p => p.OldName, p => p.NewName, StringComparer.Ordinal);
        var regex = new Regex(@"\b(" + string.Join("|", sharedPairs.Select(p => Regex.Escape(p.OldName))) + @")\b",
            RegexOptions.Compiled);
        return (map, regex);
    }

    /// <summary>
    /// Binds a simple name to a symbol, walking up through qualified names and cref references
    /// when the simple name itself does not bind (e.g. "Server.Atmos.EntitySystems.AtmosphereSystem"
    /// or <c>&lt;see cref="SharedFooSystem.Method"/&gt;</c>).
    /// </summary>
    private static ISymbol? ResolveBoundSymbol(SemanticModel model, SimpleNameSyntax sn)
    {
        var sym = model.GetSymbolInfo(sn).Symbol;
        if (sym is not null)
            return sym;

        // Walk up to the outermost qualified name first (binding partial qualified names fails).
        SyntaxNode? topQualified = null;
        for (var parent = sn.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is QualifiedNameSyntax q)
            {
                topQualified = q;
                continue;
            }

            if (topQualified is not null)
            {
                var s = model.GetSymbolInfo(topQualified).Symbol;
                if (s is not null)
                    return s;
                topQualified = null;
            }

            if (parent is CrefSyntax c)
            {
                var s = model.GetSymbolInfo(c).Symbol;
                if (s is not null)
                    return s;
                continue;
            }

            if (parent is not (NameSyntax or TypeSyntax or MemberAccessExpressionSyntax or XmlCrefAttributeSyntax))
                break;
        }

        if (topQualified is not null)
        {
            var s = model.GetSymbolInfo(topQualified).Symbol;
            if (s is not null)
                return s;
        }

        return null;
    }

    /// <summary>
    /// Computes the (assembly, namespace, name) identity of a type symbol, but only for
    /// TOP-LEVEL types. Fields/properties/locals are variable references (never renames) and
    /// nested types are never rename targets, so both yield null.
    /// </summary>
    private static (string Asm, string Ns, string Name)? TypeKeyOf(ISymbol? symbol)
    {
        if (symbol is not INamedTypeSymbol type)
            return null;
        if (type.ContainingType is not null)
            return null;

        type = type.OriginalDefinition;
        return (type.ContainingAssembly?.Name ?? "", type.ContainingNamespace?.ToString() ?? "", type.Name);
    }

    /// <summary>
    /// Computes the target file path for a renamed type, preserving a partial-file suffix
    /// (e.g. SharedActionsSystem.DoAfter.cs -> ActionsSystem.DoAfter.cs).
    /// </summary>
    private static string ComputeNewPath(string oldPath, string oldName, string newName)
    {
        var dir = Path.GetDirectoryName(oldPath) ?? "";
        var baseName = Path.GetFileNameWithoutExtension(oldPath);
        var ext = Path.GetExtension(oldPath);

        var dot = baseName.IndexOf('.');
        var first = dot >= 0 ? baseName[..dot] : baseName;
        var suffix = dot >= 0 ? baseName[dot..] : "";

        var newBase = string.Equals(first, oldName, StringComparison.Ordinal) ? newName + suffix : newName;
        return Path.Combine(dir, newBase + ext);
    }

    private static void WriteFile(string path, string content)
    {
        var bom = false;
        if (File.Exists(path))
        {
            var head = new byte[3];
            using (var fs = File.OpenRead(path))
            {
                var n = fs.Read(head, 0, 3);
                bom = n == 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF;
            }
        }
        File.WriteAllText(path, content, new UTF8Encoding(bom));
    }

    /// <summary>Moves a file with git so it is recorded as a rename, not a delete + create.</summary>
    private bool GitMv(string oldPath, string newPath, out string error)
    {
        error = "";
        if (_noGit)
        {
            File.Move(oldPath, newPath);
            return true;
        }

        var gitRoot = oldPath.StartsWith(_engineRoot, StringComparison.OrdinalIgnoreCase) ? _engineRoot : _root;
        var oldRel = Path.GetRelativePath(gitRoot, oldPath);
        var newRel = Path.GetRelativePath(gitRoot, newPath);

        try
        {
            var psi = new ProcessStartInfo("git")
            {
                WorkingDirectory = gitRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            psi.ArgumentList.Add("-C");
            psi.ArgumentList.Add(gitRoot);
            psi.ArgumentList.Add("mv");
            psi.ArgumentList.Add(oldRel);
            psi.ArgumentList.Add(newRel);

            using var proc = Process.Start(psi);
            if (proc is null)
            {
                error = "failed to start git";
                return false;
            }
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                error = stderr.Trim();
                return false;
            }

            // git mv stages the rename from the index, so re-add the new path to stage the
            // already-rewritten content (otherwise status shows RM with stale staged content).
            var add = new ProcessStartInfo("git")
            {
                WorkingDirectory = gitRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            add.ArgumentList.Add("-C");
            add.ArgumentList.Add(gitRoot);
            add.ArgumentList.Add("add");
            add.ArgumentList.Add(newRel);
            using (var addProc = Process.Start(add))
            {
                if (addProc is null)
                {
                    error = "failed to start git add";
                    return false;
                }
                var addErr = addProc.StandardError.ReadToEnd();
                addProc.WaitForExit();
                if (addProc.ExitCode != 0)
                {
                    error = addErr.Trim();
                    return false;
                }
            }
            return true;
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }
    }
}
