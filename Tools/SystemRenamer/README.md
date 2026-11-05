# SystemRenamer

One-shot Roslyn-based utility that performs the repo-wide rename described by
[space-wizards/docs#671](https://github.com/space-wizards/docs/pull/671) ("Shared types" naming
convention): **drop the `Shared` prefix from shared system classes, and give client/server
implementations their `Client`/`Server` prefix**.

Concretely, for every `EntitySystem`-derived class:

| Where | Before | After |
|---|---|---|
| Content.Shared / Robust.Shared | `SharedFooSystem` | `FooSystem` |
| Content.Server / Robust.Server | `FooSystem : SharedFooSystem` | `ServerFooSystem : FooSystem` |
| Content.Client / Robust.Client | `FooSystem : SharedFooSystem` | `ClientFooSystem : FooSystem` |

The tool renames the class declarations, **every code reference** (resolved semantically, so it
correctly disambiguates shared vs server vs client same-name types, including qualified names like
`Server.Atmos.EntitySystems.AtmosphereSystem` and field names that happen to match a type name),
partial files (`SharedActionsSystem.DoAfter.cs` → `ActionsSystem.DoAfter.cs`), and renames files
with `git mv` so history is preserved as renames rather than delete+create. It also cleans up
stale mentions in comments and doc-comment crefs.

## Requirements

- .NET 10 SDK
- A restored/buildable checkout of `space-station-14` (with `RobustToolbox` submodule)

## Usage

From the repo root:

```
dotnet run --project Tools/SystemRenamer -- --dry-run            # report only (default)
dotnet run --project Tools/SystemRenamer -- --apply             # perform the rename
dotnet run --project Tools/SystemRenamer -- --apply --dump-targets targets-dump.txt
```

### Options

| Option | Description |
|---|---|
| `--root <path>` | Repo root (default: the path baked into `Program.cs`) |
| `--apply` | Actually write changes + `git mv` files (default is dry-run) |
| `--dry-run` | Plan + write `rename-report.md`, make no changes (default) |
| `--dump-targets <path>` | Write the rename map as `Assembly\|Namespace\|Old\|New\|Kind` |
| `--report <path>` | Report output path (default `rename-report.md`) |
| `--skip <name>` | Exclude a specific target type (repeatable) |
| `--only <name>` | Restrict to matching target types (repeatable; useful for testing) |
| `--no-git` | Use `File.Move` instead of `git mv` (for repos not under git) |
| `--debug-changes` | Log every applied edit to `renamer-changes.log` |
| `--smoke` | Only load the workspace and print project/doc counts |

### After applying

1. `git add -A` in both the repo and the `RobustToolbox` submodule.
2. Build + verify:
   - `dotnet build RobustToolbox/RobustToolbox.slnx -c Debug`
   - `dotnet build SpaceStation14.slnx -c DebugOpt`
   - `git grep -n 'Shared[[:alnum:]]*System' -- '*.cs'` (remaining hits are expected: the skipped
     `SharedFoodSequenceSystem` and pre-existing stale comments)
3. Review the report (`rename-report.md`) for items skipped or flagged.

## Notes / known limitations

- **Manual follow-ups** (reported in `rename-report.md`):
  - `SharedFoodSequenceSystem` is skipped because `Content.Shared.Nutrition.EntitySystems`
    already contains a concrete `FoodSequenceSystem` (merge decision needed).
  - A few server/client systems whose name doesn't match their shared base are flagged.
- **Pre-existing namespace bug exposed by the rename**: `Content.Client.PhysicsSystem.Controllers`
  collided with the renamed `PhysicsSystem` class and had to be fixed to
  `Content.Client.Physics.Controllers` (2 files, applied manually after running the tool).
- **git rename display**: `git status`/`git diff` show only ~1000 renames by default; set
  `git config diff.renameLimit 10000` to see them all. Tiny files (a handful of one-class files)
  may still show as add+delete because git cannot compute a high enough similarity for them.
- Comments are updated for shared-system names only (`Shared*` → `*`). Comment mentions whose
  text doesn't exactly match a real type name are left alone.

## Layout

- `Program.cs` – CLI, orchestration, report generation
- `WorkspaceLoader.cs` – loads both `SpaceStation14.slnx` and `RobustToolbox.slnx` projects into one
  Roslyn solution (MSBuildWorkspace can't read `.slnx`, so a temp `.sln` is synthesized)
- `RenamePlanner.cs` – discovers targets, verifies the `EntitySystem` hierarchy, detects collisions
- `RenameApplier.cs` – semantic reference scan, edit application, file renames via `git mv`
- `cleanup-comments.ps1` – standalone comment/string cleanup pass over both repos
