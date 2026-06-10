# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

GitVersion is a multi-project .NET repository that calculates semantic versions from Git history. It supports multiple versioning strategies (GitFlow, GitHubFlow, Mainline) and integrates with CI/CD systems (GitHub Actions, Azure Pipelines, TeamCity, etc.).

## Developer commands

```bash
# Build
dotnet build ./src/GitVersion.slnx
dotnet build ./new-cli/GitVersion.slnx

# Test
dotnet test ./src/GitVersion.slnx
dotnet test --project ./src/GitVersion.Core.Tests/GitVersion.Core.Tests.csproj

# Run the legacy CLI locally
dotnet run --project src/GitVersion.App

# Run the new CLI locally
dotnet run --project new-cli/GitVersion.Cli

# Format
dotnet format ./src/GitVersion.slnx
dotnet format --verify-no-changes ./src/GitVersion.slnx   # CI check

# Regenerate schemas (after changing GitVersionVariables or GitVersionConfiguration)
./build.ps1 -Stage build -Target BuildPrepare
./build.ps1 -Stage docs -Target GenerateSchemas
```

## Architecture

The repo has two parallel solution trees:

### `src/` — legacy/stable CLI (`src/GitVersion.slnx`)

| Project | Role |
|---|---|
| `GitVersion.Core` | Core version calculation logic, version calculators, version search strategies |
| `GitVersion.Configuration` | Config loading/validation (YAML), `ConfigurationFileLocator.cs` |
| `GitVersion.App` | CLI entry point |
| `GitVersion.BuildAgents` | Platform adapters; write `GitVersion_`-prefixed env vars — preserve that prefix |
| `GitVersion.LibGit2Sharp` | Git repository access |
| `GitVersion.Output` | JSON/env/text output formatters |
| `GitVersion.MsBuild` | MSBuild task integration |
| `GitVersion.Testing` | Shared test fixtures and builders |

Key internal directories in `GitVersion.Core`:
- `VersionCalculation/VersionCalculators/` — deployment-mode calculators (Mainline, ContinuousDeployment, ContinuousDelivery)
- `VersionCalculation/VersionSearchStrategies/` — strategies for finding a base version in Git history
- `VersionCalculation/Mainline/` — mainline versioning implementation

### `new-cli/` — new CLI (`new-cli/GitVersion.slnx`, actively developed)

Plugin-based architecture: `GitVersion.Cli`, `GitVersion.Core`, `GitVersion.Calculation`, `GitVersion.Configuration`, `GitVersion.Normalization`, `GitVersion.Output`, `GitVersion.Common`, `GitVersion.Core.Libgit2Sharp`, `GitVersion.Cli.Generator`.

Each tree has its own `Directory.Packages.props` for centralized package versions.

## Conventions

- **Package versions**: update `src/Directory.Packages.props` (or `new-cli/Directory.Packages.props`), not individual csproj files. Add packages via `dotnet add package <Package> --version <Version>`.
- **Config file names**: `GitVersion.yml`, `GitVersion.yaml`, `.GitVersion.yml`, `.GitVersion.yaml` — see `ConfigurationFileLocator.cs` for the lookup order.
- **Code style**: defined in `.editorconfig`; run `dotnet format` to apply. C# latest features, nullable reference types, implicit usings enabled.
- **Commit style**: prefer atomic commits; rebase onto `main` rather than merging.
- **CLI output changes**: update `docs/` examples and build-agent adapters that parse JSON or env vars.

## Testing

Integration tests live in `src/GitVersion.Core.Tests/IntegrationTests/` with a scenario class per branch type (`MainScenarios`, `FeatureBranchScenarios`, `ReleaseScenarios`, etc.). Use `fixture.AssertFullSemver("x.y.z-label.n", configuration)` to assert calculated versions.

```csharp
using var fixture = new EmptyRepositoryFixture();
fixture.Repository.MakeATaggedCommit("1.0.0");
fixture.Repository.CreateBranch("feature/my-feature");
fixture.Checkout("feature/my-feature");  // use fixture.Checkout(), not fixture.Repository.Checkout()
fixture.Repository.MakeACommit();

var configuration = GitFlowConfigurationBuilder.New.Build();
fixture.AssertFullSemver("1.0.1-my-feature.1", configuration);
```

Test stack: NUnit 4.x, Shouldly assertions, NSubstitute mocks, `EmptyRepositoryFixture` / `BaseGitFlowRepositoryFixture`, config builders (`GitFlowConfigurationBuilder`, `GitHubFlowConfigurationBuilder`, `EmptyConfigurationBuilder`).