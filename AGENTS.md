# GitVersion — AI Agent Instructions

This file provides repo-specific guidance for AI coding agents (e.g. Claude Code, Codex, Copilot Workspace).

## Project overview

GitVersion is a multi-project .NET repository that calculates semantic versions from Git history.
Primary source code lives under `src/`. CLI examples and documentation live under `docs/`.

## Key files

- `README.md` — project overview and links to documentation
- `global.json` — SDK version pin (.NET 10) and solution roots (`build`, `new-cli`, `src`)
- `build.ps1` — primary build entry point (Cake-based); day-to-day work uses `dotnet` CLI directly
- `src/Directory.Packages.props` — central NuGet package versioning (edit here, not in individual csproj files)
- `src/GitVersion.slnx` — main solution file
- `docs/` — CLI usage examples and I/O patterns (JSON stdout, environment outputs)
- `src/GitVersion.Configuration/ConfigurationFileLocator.cs` — config file lookup logic

## Architecture

The repo has two parallel solution trees:

### `src/` — legacy/stable CLI

| Project                    | Role                                   |
| -------------------------- | -------------------------------------- |
| `GitVersion.Core`          | Core version calculation logic         |
| `GitVersion.Configuration` | Configuration loading and validation   |
| `GitVersion.App`           | CLI entry point                        |
| `GitVersion.BuildAgents`   | Platform-specific build agent adapters |
| `GitVersion.LibGit2Sharp`  | Git repository access                  |
| `*Tests` projects          | Unit and integration tests             |

Build-agent adapters live in `src/GitVersion.BuildAgents/Agents/`. They write `GitVersion_`-prefixed environment variables — preserve that prefix when reading or writing outputs.

### `new-cli/` — new CLI (actively developed, `new-cli/GitVersion.slnx`)

| Project                          | Role                                       |
| -------------------------------- | ------------------------------------------ |
| `GitVersion.Cli`                 | New CLI entry point                        |
| `GitVersion.Core`                | Core version calculation (new-cli variant) |
| `GitVersion.Calculation`         | Version calculation plugin                 |
| `GitVersion.Configuration`       | Configuration plugin                       |
| `GitVersion.Normalization`       | Normalization plugin                       |
| `GitVersion.Output`              | Output plugin                              |
| `GitVersion.Common`              | Shared utilities                           |
| `GitVersion.Core.Libgit2Sharp`   | Git repository access                      |
| `GitVersion.Cli.Generator`       | Source generator for CLI commands          |
| `GitVersion.Cli.Generator.Tests` | Generator tests                            |

The `new-cli/` tree has its own `Directory.Packages.props` for centralized package versions.

## Developer commands

```bash
# --- src/ (legacy CLI) ---

# Build the solution
dotnet build ./src/GitVersion.slnx

# Run all tests
dotnet test ./src/GitVersion.slnx

# Run tests for a single project
dotnet test --project ./src/GitVersion.Core.Tests/GitVersion.Core.Tests.csproj

# Run the legacy CLI locally
dotnet run --project src/GitVersion.App

# Format code
dotnet format ./src/GitVersion.slnx

# Verify formatting (CI-friendly, non-zero exit if changes needed)
dotnet format --verify-no-changes ./src/GitVersion.slnx

# --- new-cli/ (new CLI) ---

# Build the new CLI solution
dotnet build ./new-cli/GitVersion.slnx

# Run tests for the new CLI
dotnet test ./new-cli/GitVersion.slnx

# Run the new CLI locally
dotnet run --project new-cli/GitVersion.Cli
```

## Conventions

- **SDK / TFM**: .NET 10 (`global.json`); most projects target `net10.0`.
- **Package versions**: update `src/Directory.Packages.props`, not individual csproj files. Add packages via `dotnet add package <Package> --version <Version>`.
- **Config file names**: `GitVersion.yml`, `GitVersion.yaml`, `.GitVersion.yml`, `.GitVersion.yaml` — use these names or pass `--configfile`.
- **Code style**: `.editorconfig` defines style; run `dotnet format` to apply.
- **Commit style**: prefer atomic commits; rebase onto `main` rather than merging.
- **Tests**: integration tests live in `src/GitVersion.Core.Tests/IntegrationTests/`. Use `EmptyRepositoryFixture` / `BaseGitFlowRepositoryFixture` and builder patterns (`GitFlowConfigurationBuilder`, `GitHubFlowConfigurationBuilder`).

## What to check when changing behavior

- CLI output shape changed → update `docs/` examples and build-agent adapters that parse JSON or env vars.
- New dependency added → update `src/Directory.Packages.props` and verify with `dotnet build`.
- Configuration schema changed → regenerate schemas:

  ```bash
  ./build.ps1 -Stage build -Target BuildPrepare
  ./build.ps1 -Stage docs -Target GenerateSchemas
  ```

## Testing guidance

Most relevant tests are in `src/GitVersion.Core.Tests/IntegrationTests/`. There is a scenario class per branch type (e.g. `MainScenarios`, `FeatureBranchScenarios`). Use `fixture.AssertFullSemver("x.y.z-label.n", configuration)` to assert calculated versions.

```csharp
using var fixture = new EmptyRepositoryFixture();
fixture.Repository.MakeATaggedCommit("1.0.0");
fixture.Repository.CreateBranch("feature/my-feature");
fixture.Checkout("feature/my-feature");  // use fixture.Checkout(), not fixture.Repository.Checkout()
fixture.Repository.MakeACommit();

var configuration = GitFlowConfigurationBuilder.New.Build();
fixture.AssertFullSemver("1.0.1-my-feature.1", configuration);
```
