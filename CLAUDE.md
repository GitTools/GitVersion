# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

For configuration work, follow the [configuration validation sequence in AGENTS.md](AGENTS.md#what-to-check-when-changing-behavior): validate when the selected executable supports it, inspect effective settings, then verify representative history. Older tools still need matching-version schema and loader checks.

GitVersion is a multi-project .NET repository that calculates semantic versions from Git history. It supports multiple versioning strategies (GitFlow, GitHubFlow, Mainline) and integrates with CI/CD systems (GitHub Actions, Azure Pipelines, TeamCity, etc.).

## CodeRabbit reviews

Follow the shared [CodeRabbit review workflow in AGENTS.md](AGENTS.md#coderabbit-reviews)
after completing changes, including scope selection, validation of findings,
reviewing fixes, and reporting authentication or service-limit blockers.

Install the Claude Code plugin from the repository root with
`claude plugin install coderabbit --scope project`. If the plugin cannot be found,
add its official marketplace with `claude plugin marketplace add coderabbitai/claude-plugin`
and retry. See the [official Claude Code integration guide](https://docs.coderabbit.ai/cli/claude-code-integration).
The plugin uses the same authenticated CodeRabbit CLI as the Codex integration.

Use `/coderabbit:review uncommitted` for local edits, adding `--include-untracked`
when new files belong to the change. For a branch review, use
`/coderabbit:review committed --base origin/main`, or the task's requested base.
When using the CLI directly, pass this context file explicitly, for example:
`coderabbit review --agent --uncommitted -c CLAUDE.md`.

Present the proposed fixes, then apply confirmed findings within the authorized
task scope and run relevant validation. Keep changes local unless delivery has
been authorized, and report any remaining findings or review blockers.

## Developer commands

```bash
# Build
dotnet build ./src/GitVersion.slnx
dotnet build ./new-cli/GitVersion.slnx

# Test
dotnet test ./src/GitVersion.slnx
dotnet test --project ./src/GitVersion.Core.Tests/GitVersion.Core.Tests.csproj

# Run the legacy CLI locally
dotnet run --project src/GitVersion.App --framework net10.0

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
- **Code style**: defined in `.editorconfig`; run `dotnet format` to apply. Nullable reference types and implicit usings are enabled.
- **C# version**: `LangVersion=latest` (C# 14). Prefer new syntax where it improves clarity:
  - `field` keyword — access auto-property backing field inside the property body instead of a manual backing field
  - Extension members — use the new `extension(Type t) { }` block syntax for extension methods/properties
  - Null-conditional assignment — `x?.Property = value`
  - `params` collections — `params` now works with any collection type, not just arrays
  - Partial properties — analogous to partial methods for source generators
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

<!-- jbcontext-instructions-start -->
# Tools

## Semantic Code Search (jbcontext)

You have access to the jbcontext MCP `code_search` tool for searching the codebase semantically.
This tool can search for code snippets related in meaning to the search query and search objective.

### When to use

`code_search` is a **code-discovery** tool. Reach for it only when a task requires finding or understanding code whose location you don't already know.

Skip it - go straight to the right tool - when:

- the task names the exact file, class, or symbol (keyword grep is faster);
- the relevant file is already open or identified;
- the task doesn't involve locating code at all - git operations (rebase, merge, commit), running tests or builds, shell/statusline/config setup, or reviewing a diff you already have.

### Single-Shot Policy

When the relevant file or subsystem *is* still unknown, use `code_search` as the semantic bootstrap:

- If no relevant file is open yet, start with one `code_search`.
- Make the first query specific to the issue's named feature, class, method, config flag, or behavior when available.
- After the first search, open at least one returned file and inspect it locally.
- If the first hit is relevant but incomplete, inspect neighboring files locally in that same directory or subsystem before any semantic retry.
- After the first relevant file or path is known, prefer direct file reads and exact search to inspect nearby code.
- If a semantic retry is still needed, set `pathFilter` to the directory of the best first hit.

<!-- jbcontext-instructions-end -->
