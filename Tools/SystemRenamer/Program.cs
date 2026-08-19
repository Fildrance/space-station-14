using System.Text;
using SystemRenamer;

var root = @"e:\Sources\Repos\ss14\space-station-14";
var apply = false;
var smoke = false;
var noGit = false;
var reportPath = Path.Combine(root, "rename-report.md");
var dumpPath = (string?)null;
var skip = new List<string>();
var only = new List<string>();

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--root" when i + 1 < args.Length: root = args[++i]; break;
        case "--apply": apply = true; break;
        case "--smoke": smoke = true; break;
        case "--no-git": noGit = true; break;
        case "--report" when i + 1 < args.Length: reportPath = args[++i]; break;
        case "--dump-targets" when i + 1 < args.Length: dumpPath = args[++i]; break;
        case "--skip" when i + 1 < args.Length: skip.Add(args[++i]); break;
        case "--only" when i + 1 < args.Length: only.Add(args[++i]); break;
    }
}

Console.WriteLine($"Root: {root}");
Console.WriteLine("Loading workspace (this may take a minute)...");
var loader = new WorkspaceLoader(root);
var solution = await loader.LoadAsync();

if (smoke)
{
    Console.WriteLine($"Solution: {solution.Projects.Count()} projects, {solution.Projects.Sum(p => p.Documents.Count())} documents.");
    return 0;
}

Console.WriteLine("Planning renames...");
var planner = new RenamePlanner(solution, skip, only.Count > 0 ? only : null);
var plan = await planner.PlanAsync();

var report = BuildReport(plan, apply);
await File.WriteAllTextAsync(reportPath, report, new UTF8Encoding(false));
Console.WriteLine($"Report written to {reportPath}");

if (dumpPath is not null)
{
    var sb = new StringBuilder();
    foreach (var t in plan.Targets.OrderBy(t => t.AssemblyName).ThenBy(t => t.OldName))
        sb.AppendLine($"{t.AssemblyName}|{t.TypeNamespace}|{t.OldName}|{t.NewName}|{t.Kind}");
    await File.WriteAllTextAsync(dumpPath, sb.ToString());
    Console.WriteLine($"Targets dumped to {dumpPath}");
}

if (!apply)
{
    Console.WriteLine("Dry-run complete. Re-run with --apply to make changes.");
    return 0;
}

Console.WriteLine("Applying renames...");
var applier = new RenameApplier(solution, plan.Targets, root, noGit, args.Contains("--debug-changes"));
var result = await applier.ApplyAsync();

Console.WriteLine();
Console.WriteLine($"Done. Documents rewritten: {result.DocumentsWritten}, files renamed: {result.FilesRenamed}.");
foreach (var (asm, count) in result.EditsPerAssembly.OrderBy(kv => kv.Key))
    Console.WriteLine($"  {asm}: {count} files touched");

if (result.Failures.Count > 0)
{
    Console.WriteLine($"\n{result.Failures.Count} failures:");
    foreach (var f in result.Failures.Take(50))
        Console.WriteLine($"  {f}");
}
if (result.GitMvFailures.Count > 0)
{
    Console.WriteLine($"\n{result.GitMvFailures.Count} git mv failures:");
    foreach (var f in result.GitMvFailures.Take(50))
        Console.WriteLine($"  {f}");
}

Console.WriteLine("\nNext steps:");
Console.WriteLine("  git -C \"{0}\" add -A", Path.Combine(root, "RobustToolbox"));
Console.WriteLine("  git -C \"{0}\" add -A", root);
Console.WriteLine("  Review: git -C \"{0}\" status", root);

return 0;

static string BuildReport(RenamePlan plan, bool applied)
{
    var sb = new StringBuilder();
    sb.AppendLine("# System rename report");
    sb.AppendLine();
    sb.AppendLine($"Mode: **{(applied ? "applied" : "dry-run")}**");
    sb.AppendLine();
    sb.AppendLine("## Summary");
    sb.AppendLine();
    sb.AppendLine($"| Kind | Count |");
    sb.AppendLine($"|---|---|");
    sb.AppendLine($"| Shared (`Shared*System` → `*System`) | {plan.Shared.Count} |");
    sb.AppendLine($"| Server (`*System` → `Server*System`) | {plan.Server.Count} |");
    sb.AppendLine($"| Client (`*System` → `Client*System`) | {plan.Client.Count} |");
    sb.AppendLine($"| **Total** | **{plan.Targets.Count}** |");
    sb.AppendLine();

    sb.AppendLine("### By assembly");
    sb.AppendLine();
    sb.AppendLine("| Assembly | Shared | Server | Client |");
    sb.AppendLine("|---|---|---|---|");
    foreach (var asm in plan.Targets.Select(t => t.AssemblyName).Distinct().OrderBy(a => a))
    {
        var t = plan.Targets.Where(x => x.AssemblyName == asm).ToList();
        sb.AppendLine($"| {asm} | {t.Count(x => x.Kind == "shared")} | {t.Count(x => x.Kind == "server")} | {t.Count(x => x.Kind == "client")} |");
    }
    sb.AppendLine();

    if (plan.Skipped.Count > 0)
    {
        sb.AppendLine("## Skipped (require manual attention)");
        sb.AppendLine();
        foreach (var (target, reason) in plan.Skipped)
            sb.AppendLine($"- `{target.TypeNamespace}.{target.OldName}` → `{target.NewName}` — {reason}");
        sb.AppendLine();
    }

    var review = plan.Targets.Where(t => t.ReviewNote is not null).ToList();
    if (review.Count > 0)
    {
        sb.AppendLine("## Renamed but flagged for review");
        sb.AppendLine();
        foreach (var t in review)
            sb.AppendLine($"- `{t.OldName}` → `{t.NewName}` ({t.Kind}) — {t.ReviewNote}");
        sb.AppendLine();
    }

    sb.AppendLine("## Verification");
    sb.AppendLine();
    sb.AppendLine("1. Build the engine: `dotnet build RobustToolbox/RobustToolbox.slnx -c Debug`");
    sb.AppendLine("2. Build the game:  `dotnet build SpaceStation14.slnx -c DebugOpt`");
    sb.AppendLine("3. Grep for leftovers: `git grep -n 'Shared[[:alnum:]]*System' -- '*.cs'`");
    return sb.ToString();
}
