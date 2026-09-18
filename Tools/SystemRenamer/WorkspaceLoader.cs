using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace SystemRenamer;

/// <summary>
/// Loads all projects from both SpaceStation14.slnx and RobustToolbox.slnx into a single
/// Roslyn solution so that symbols and references can be resolved across engine and content.
/// </summary>
public sealed class WorkspaceLoader
{
    private readonly string _root;

    public WorkspaceLoader(string root)
    {
        _root = Path.GetFullPath(root);
    }

    public string Root => _root;
    public string EngineRoot => Path.Combine(_root, "RobustToolbox");

    /// <summary>
    /// Opens every managed project from both solution files. Returns the combined solution.
    /// </summary>
    public async Task<Solution> LoadAsync()
    {
        if (!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();

        var tempSln = BuildTempSolutionFile();
        using var workspace = MSBuildWorkspace.Create();

        var solution = await workspace.OpenSolutionAsync(tempSln);
        Console.WriteLine($"Loaded {solution.Projects.Count()} projects from {tempSln}");
        return solution;
    }

    /// <summary>
    /// MSBuildWorkspace does not understand .slnx, so we synthesize a classic .sln in %TEMP%
    /// containing every managed project from both solution files.
    /// </summary>
    private string BuildTempSolutionFile()
    {
        var projects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sln in new[]
                 {
                     Path.Combine(_root, "SpaceStation14.slnx"),
                     Path.Combine(EngineRoot, "RobustToolbox.slnx"),
                 })
        {
            if (!File.Exists(sln))
                continue;
            var text = File.ReadAllText(sln);
            foreach (Match m in Regex.Matches(text, @"<Project\s+Path=""([^""]+\.csproj)"""))
            {
                var full = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sln)!, m.Groups[1].Value));
                if (File.Exists(full))
                    projects.Add(full);
            }
        }

        // Deterministic order: engine core first so project references resolve, then content.
        var ordered = projects
            .OrderBy(p => p.Contains(Path.Combine("RobustToolbox", "Robust.Shared")) ? 0
                : p.Contains(Path.Combine("RobustToolbox", "Robust.Server")) ? 1
                : p.Contains(Path.Combine("RobustToolbox", "Robust.Client")) ? 2
                : p.Contains(Path.Combine("RobustToolbox", "Robust.Server.Testing")) ? 3
                : p.Contains("RobustToolbox") ? 4
                : p.Contains(Path.Combine("Content.Shared")) ? 5
                : p.Contains(Path.Combine("Content.Server")) ? 6
                : p.Contains(Path.Combine("Content.Client")) ? 7
                : 8)
            .ThenBy(p => p)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
        sb.AppendLine("# Visual Studio Version 17");

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in ordered)
        {
            var name = Path.GetFileNameWithoutExtension(project);
            var unique = name;
            var i = 1;
            while (!seenNames.Add(unique))
                unique = $"{name}_{i++}";
            var guid = Guid.NewGuid().ToString("B").ToUpperInvariant();
            var path = Path.GetRelativePath(Path.GetTempPath(), project);
            sb.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{unique}\", \"{path}\", \"{guid}\"");
            sb.AppendLine("EndProject");
        }
        sb.AppendLine("Global");
        sb.AppendLine("EndGlobal");

        var path2 = Path.Combine(Path.GetTempPath(), $"SystemRenamer-{Guid.NewGuid():N}.sln");
        File.WriteAllText(path2, sb.ToString());
        return path2;
    }
}
