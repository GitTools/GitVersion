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

## Code discovery: context-explorer first

When a task requires finding or understanding code whose location you don't
already know, your FIRST code-discovery step MUST be:

Task(subagent_type='context-explorer',
     description=<short label>,
     prompt=<1-2 sentence intent describing what to find>)

Start there instead of opening with your own `grep`/`glob`/`bash` searches or
git history: the subagent runs the semantic exploration in its own context and
hands back concrete `file:line` references, so you don't burn your context
re-reading the same files.

This governs *how* you begin code discovery — not whether every task needs it.
Do NOT call context-explorer when the task doesn't involve locating code:

- the task names the exact file, class, or symbol — open it or grep directly;
- the relevant file is already open or identified;
- the work is a git operation (rebase, merge, commit), a test/build run,
  shell/statusline/config setup, or a review of a diff you already have.

Invoking context-explorer as a formality "to get started" on such tasks wastes
a subagent round and returns irrelevant findings. It is a research step, not a
gate to clear — skip it and proceed directly.

When you do use it, the subagent runs up to 3 semantic searches in its own
context (restricted to `jbcontext search` via `Bash` and `Read` only) and
returns a short report:

Searched: <one-line summary>
Findings:
- <relative/path>:<line> — <description>
- ...
Notes: <confidence; whether keyword grep would be more direct here>

Use its findings if they look useful, or ignore them entirely if `Notes:` flags
the task as keyword-based. You retain full freedom for the rest of the run.

## Semantic Code Search (jbcontext)

You have access to `jbcontext search` for searching the codebase semantically.
It finds code by meaning, not just keywords.

### Usage

```bash
jbcontext search "<detailed and descriptive query>"
jbcontext search -p <path> "<query>"  # <path> must be relative to the project root
```

### Query Tips

- Be descriptive: "function that validates user email addresses" > "email"
- Include context: "error handling middleware for HTTP requests with logging"
- Specify what you're looking for: "React component that renders a modal dialog"

### Single-Shot Policy

Use `jbcontext search` as a semantic bootstrap when the relevant file or subsystem is still unknown.

- If no relevant file is open yet, start with one `jbcontext search`.
- Make the first query specific to the issue's named feature, class, method, config flag, or behavior when available.
- After the first search, open at least one returned file and inspect it locally.
- If the first hit is relevant but incomplete, inspect neighboring files locally in that same directory or subsystem before any semantic retry.
- After the first relevant file or path is known, prefer direct file reads and exact search to inspect nearby code.
- If a semantic retry is still needed, use `jbcontext search -p <path> ...` with the directory of the best first hit.

### Examples

```bash
# Find authentication-related code
jbcontext search "user authentication login flow"

# Narrow to specific directory
jbcontext search -p src/auth "JWT token validation"
```

Use `jbcontext search` once to get the initial pointer, then inspect nearby code locally. If that still fails, do a narrowed retry with `-p`.
<!-- jbcontext-instructions-end -->