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
- **C# version**: `LangVersion=latest` (C# 14). Prefer new syntax where it improves clarity:
  - `field` keyword — access auto-property backing field inside the property body instead of a manual backing field
  - Extension members — use the new `extension(Type t) { }` block syntax for extension methods/properties
  - Null-conditional assignment — `x?.Property = value`
  - `params` collections — `params` now works with any collection type, not just arrays
  - Partial properties — analogous to partial methods for source generators
- **Package versions**: update `src/Directory.Packages.props`, not individual csproj files. Add packages via `dotnet add package <Package> --version <Version>`.
- **Config file names**: `GitVersion.yml`, `GitVersion.yaml`, `.GitVersion.yml`, `.GitVersion.yaml` — use these names or pass `--configfile`.
- **Code style**: `.editorconfig` defines style; run `dotnet format` to apply.
- **Commit style**: prefer atomic commits; rebase onto `main` rather than merging.
- **Tests**: integration tests live in `src/GitVersion.Core.Tests/IntegrationTests/`. Use `EmptyRepositoryFixture` / `BaseGitFlowRepositoryFixture` and builder patterns (`GitFlowConfigurationBuilder`, `GitHubFlowConfigurationBuilder`).

## Release process

Cutting a release (milestone setup, label validation, creating the GitHub release, monitoring downstream
publish PRs for Homebrew/winget/GitTools Actions, and verifying published artifacts on NuGet/Docker/Chocolatey)
is documented step-by-step in [`.agents/skills/release/SKILL.md`](.agents/skills/release/SKILL.md) — read that
file in full before doing any release work, and follow its phases in order rather than improvising. It's also
symlinked at `.claude/skills/release` for tool discovery, and summarized for humans in
[`CONTRIBUTING.md`](CONTRIBUTING.md#release-process). It requires the `gh` CLI authenticated (`gh auth login`).

## CodeRabbit reviews

Use CodeRabbit to review completed changes before handing them back. Prefer the
CodeRabbit plugin for your agent when installed; otherwise use the CLI from the worktree
containing the changes. Follow the [official setup guide](https://docs.coderabbit.ai/cli/codex-integration)
to install the CLI and plugin, then authenticate with your account's hosting region
(`coderabbit auth login --agent --region us` or `--region eu`). Authentication is
per developer and must never be committed to this repository.

- Check `coderabbit auth status --agent` before starting a review. If installation,
  authentication, or service limits block the review, report that limitation;
  do not describe it as a passing review.
- Summarize the diff and choose an explicit scope. For local edits, run
  `coderabbit review --agent --uncommitted`. Add `--include-untracked` when new
  files belong to the change. For committed branch changes, run
  `coderabbit review --agent --committed --base origin/main` after refreshing the
  base ref, or use the task's explicitly requested base.
- Wait for completion. Report the finding count, severity, file location, impact,
  and recommended fix for actionable findings. Check each finding against the
  current code before changing it.
- Fix confirmed issues within the task's scope, run relevant validation, and
  review the resulting changes again. Explain dismissed findings and report
  remaining blockers. Stop on repeated findings or service limits rather than
  looping indefinitely; do not opt into usage credits without authorization.
- Focus on version calculation and branch/tag semantics, compatibility of CLI
  and build-agent outputs, consistency between Git backends, generated
  configuration/documentation, and regression coverage for changed behavior.
  Apply the repository conventions above to all review fixes.
- Keep fixes local unless the user has authorized committing, pushing, or
  opening a pull request. CodeRabbit complements the repository's tests and CI.

## Tips

- For `gh` commands, set `GH_PAGER=cat GH_FORCE_TTY=0` to avoid pager/TTY issues in non-interactive terminals.

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

<!-- jbcontext-instructions-start -->
# Tools

## Semantic Code Search (jbcontext)

You have access to `jbcontext search` for searching the codebase semantically.
Use the `/context-search` skill or run `jbcontext search "<query>"` to find code by meaning, not just keywords.

### Query Tips

- Be descriptive: "Where is a function that validates user email addresses" > "email"
- Include context: "Find error handling middleware for HTTP requests with logging"
- Specify what you're looking for: "React component that renders a modal dialog"

### When to use

`jbcontext search` is a **code-discovery** tool. Reach for it only when a task requires finding or understanding code whose location you don't already know.

Skip it — go straight to the right tool — when:
- the task names the exact file, class, or symbol (keyword grep is faster);
- the relevant file is already open or identified;
- the task doesn't involve locating code at all — git operations (rebase, merge, commit), running tests or builds, shell/statusline/config setup, or reviewing a diff you already have.

### How to use it
- Start with `jbcontext search` before planning, editing, or exact search in unfamiliar code when you do not yet know the right file, subsystem, implementation, or related test.
- Use one focused natural-language query per search.
- Do not start with grep, ripgrep, or find when the search problem is still semantic or exploratory.
- Inspect the first relevant file or directory before issuing another broad semantic search.
- Use another broad `jbcontext search` only if the local path stops being productive.
- Once you know the relevant file, symbol, or directory, switch to direct file reads or exact search for local inspection.
- If you search again after finding a relevant area, narrow with `-p <path>`.

<!-- jbcontext-instructions-end -->
