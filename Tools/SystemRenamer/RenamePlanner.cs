using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SystemRenamer;

/// <summary>A single type that should be renamed.</summary>
public sealed record RenameTarget(
    INamedTypeSymbol Symbol,
    string OldName,
    string NewName,
    string Kind, // "shared" | "server" | "client"
    string AssemblyName,
    string TypeNamespace,
    string? ReviewNote = null);

/// <summary>The result of planning: what to rename and what was skipped and why.</summary>
public sealed record RenamePlan(List<RenameTarget> Targets, List<(RenameTarget Target, string Reason)> Skipped)
{
    public IReadOnlyList<RenameTarget> Shared => Targets.Where(t => t.Kind == "shared").ToList();
    public IReadOnlyList<RenameTarget> Server => Targets.Where(t => t.Kind == "server").ToList();
    public IReadOnlyList<RenameTarget> Client => Targets.Where(t => t.Kind == "client").ToList();
}

/// <summary>
/// Discovers every system class that needs renaming under the new conventions
/// (no <c>Shared</c> prefix on shared types; <c>Server</c>/<c>Client</c> prefixes on
/// client/server implementations of shared types), and detects collisions.
/// </summary>
public sealed class RenamePlanner
{
    private static readonly string[] SharedAssemblies = { "Content.Shared", "Robust.Shared" };
    private static readonly string[] ServerAssemblies = { "Content.Server", "Robust.Server" };
    private static readonly string[] ClientAssemblies = { "Content.Client", "Robust.Client" };

    private readonly Solution _solution;
    private readonly HashSet<string> _skip;
    private readonly HashSet<string>? _only;

    public RenamePlanner(Solution solution, IEnumerable<string> skip, IEnumerable<string>? only = null)
    {
        _solution = solution;
        _skip = new HashSet<string>(skip, StringComparer.Ordinal);
        _only = only is null ? null : new HashSet<string>(only, StringComparer.Ordinal);
    }

    public async Task<RenamePlan> PlanAsync()
    {
        var targets = new List<RenameTarget>();
        var sharedByKey = new Dictionary<(string asm, string ns, string name), RenameTarget>();

        // --- Shared targets: classes named Shared*System in shared assemblies, EntitySystem-derived ---
        foreach (var project in _solution.Projects)
        {
            if (!SharedAssemblies.Contains(project.AssemblyName))
                continue;

            foreach (var doc in project.Documents)
            {
                var root = await doc.GetSyntaxRootAsync();
                if (root is null)
                    continue;

                var model = await doc.GetSemanticModelAsync();
                foreach (var cls in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var name = cls.Identifier.ValueText;
                    if (!name.StartsWith("Shared") || !name.EndsWith("System"))
                        continue;
                    if (_skip.Contains(name))
                        continue;

                    var sym = model?.GetDeclaredSymbol(cls) as INamedTypeSymbol;
                    if (sym is null || sym.ContainingType is not null)
                        continue;
                    if (!sym.IsEntitySystemDerived())
                        continue;

                    var target = new RenameTarget(sym, name, name["Shared".Length..], "shared",
                        project.AssemblyName, sym.ContainingNamespace?.ToString() ?? "");
                    targets.Add(target);
                    sharedByKey[(project.AssemblyName, target.TypeNamespace, sym.MetadataName)] = target;
                }
            }
        }

        // --- Server / Client targets: unprefixed *System classes in client/server assemblies
        //     that derive (transitively) from a Shared*System target ---
        foreach (var project in _solution.Projects)
        {
            string? kind = null;
            if (ServerAssemblies.Contains(project.AssemblyName))
                kind = "server";
            else if (ClientAssemblies.Contains(project.AssemblyName))
                kind = "client";
            if (kind is null)
                continue;

            var prefix = kind == "server" ? "Server" : "Client";

            foreach (var doc in project.Documents)
            {
                var root = await doc.GetSyntaxRootAsync();
                if (root is null)
                    continue;

                var model = await doc.GetSemanticModelAsync();
                foreach (var cls in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var name = cls.Identifier.ValueText;
                    if (!name.EndsWith("System"))
                        continue;
                    if (name.StartsWith("Shared") || name.StartsWith("Server") || name.StartsWith("Client"))
                        continue;
                    if (_skip.Contains(name))
                        continue;

                    var sym = model?.GetDeclaredSymbol(cls) as INamedTypeSymbol;
                    if (sym is null || sym.ContainingType is not null)
                        continue;

                    string? reviewNote = null;
                    var found = false;
                    for (var b = sym.BaseType; b is not null; b = b.BaseType)
                    {
                        var key = (b.ContainingAssembly?.Name ?? "", b.ContainingNamespace?.ToString() ?? "", b.MetadataName);
                        if (sharedByKey.TryGetValue(key, out _))
                        {
                            found = true;
                            if (b.MetadataName != "Shared" + name)
                                reviewNote = $"name does not match base ({b.MetadataName})";
                            break;
                        }
                    }

                    if (!found)
                        continue;

                    targets.Add(new RenameTarget(sym, name, prefix + name, kind,
                        project.AssemblyName, sym.ContainingNamespace?.ToString() ?? "", reviewNote));
                }
            }
        }

        // Deduplicate: partial class declarations produce the same symbol multiple times.
        var seenSymbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        targets = targets.Where(t => seenSymbols.Add(t.Symbol)).ToList();

        if (_only is not null)
            targets = targets.Where(t => _only.Contains(t.OldName) || _only.Contains(t.NewName)).ToList();

        // --- Collision detection (same namespace already contains the new name) ---
        var skipped = new List<(RenameTarget Target, string Reason)>();
        foreach (var target in targets)
        {
            var ns = target.Symbol.ContainingNamespace;
            if (ns is null)
                continue;

            var existing = ns.GetTypeMembers(target.NewName)
                .Where(m => !targets.Any(t => SymbolEqualityComparer.Default.Equals(t.Symbol, m)))
                .Select(m => m.ToDisplayString())
                .ToList();

            if (existing.Count > 0)
            {
                skipped.Add((target, $"collides with existing type {existing[0]} in the same namespace"));
                continue;
            }

            // Two distinct targets mapping to the same new name in the same namespace.
            var same = targets.Count(t => !ReferenceEquals(t, target)
                && t.AssemblyName == target.AssemblyName
                && t.TypeNamespace == target.TypeNamespace
                && t.NewName == target.NewName);
            if (same > 0)
            {
                skipped.Add((target, $"multiple targets would become {target.NewName} in {target.TypeNamespace}"));
            }
        }

        var kept = targets.Where(t => !skipped.Any(s => ReferenceEquals(s.Target, t))).ToList();
        return new RenamePlan(kept, skipped);
    }
}

internal static class SymbolExtensions
{
    /// <summary>Walks the base type chain looking for Robust.Shared's <c>EntitySystem</c>.</summary>
    public static bool IsEntitySystemDerived(this INamedTypeSymbol symbol)
    {
        for (var t = symbol; t is not null; t = t.BaseType)
        {
            if (t.Name == "EntitySystem" && t.ContainingAssembly?.Name == "Robust.Shared")
                return true;
        }
        return false;
    }
}
