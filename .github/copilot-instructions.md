<!-- Copilot instructions for GitVersion repository -->
# GitVersion — Copilot agent instructions

Purpose: give an AI coding agent the minimal, repo-specific knowledge needed to be productive.

- **Big picture**: This is a multi-project .NET repository. Primary source code lives under `src/` (many projects: `GitVersion.App`, `GitVersion.Core`, `GitVersion.Configuration`, build-agent adapters, and `*Tests` projects). CLI examples and docs live under `docs/`.

- **Key files**:
  - `README.md` — project overview and links to docs. See [README.md](README.md#L1).
  - `global.json` — SDK version and solution roots (`build`, `new-cli`, `src`). See [global.json](global.json#L1).
  - `build.ps1` — primary build entry. Prefer using the `dotnet` CLI to build and test projects under `src/`. Build stages exist under `build/` if you need them, but day-to-day work should target the `src` solution or individual projects. See [build.ps1](build.ps1#L1).
  - `src/Directory.Packages.props` — central package versioning (important when adding/upgrading NuGet deps). See [src/Directory.Packages.props](src/Directory.Packages.props#L1).
  - `docs/` contain CLI examples and I/O patterns (JSON on stdout, environment outputs). See [docs](docs#L1).
  - `src/GitVersion.Configuration/ConfigurationFileLocator.cs` — shows how config files are located and supported names (`GitVersion.yml`, `.GitVersion.yml`, `.yaml` variants).

- **Architecture summary (short)**:
  - `src/` contains modular .NET projects: core version calculation, configuration, output generators, and build-agent integrations.

- **Developer workflows & concrete commands**:
  - Build & test (use `dotnet` directly against the solution/projects in `src`):
    - Build the main solution:

      ```bash
      # Build the entire solution
      dotnet build ./src/GitVersion.slnx

      # Or build a single project
      dotnet build --project ./src/GitVersion.Core/GitVersion.Core.csproj
      ```

    - Run tests for the repository:

      ```bash
      # Run tests for the entire solution (pass the solution file)
      dotnet test ./src/GitVersion.slnx

      # Or run tests for a single project using --project
      dotnet test --project ./src/GitVersion.Core/GitVersion.Core.csproj
      ```
  - Run tests (solution under `src`):
    - `dotnet test ./src/GitVersion.slnx` or `dotnet test --project <path-to-csproj>`
  - Run the CLI locally (build & run project):
    - `dotnet run --project src/GitVersion.App`
    - Refer to `docs/` for examples and I/O patterns.

  - Formatting:
    - Use `dotnet format` to keep code style consistent across the repo. Example commands:

      ```bash
      # restore any local tools (if configured)
      dotnet tool restore

      # format the solution in-place
      dotnet format ./src/GitVersion.slnx

      # CI-friendly check (exit non-zero when formatting needed)
      dotnet format --verify-no-changes ./src/GitVersion.slnx
      ```

- **Conventions & patterns to follow**:
  - SDK/TFM: repo uses .NET 10 in `global.json` and many projects target `net10.0`. Respect `Directory.Packages.props` when adding dependencies.
  - Centralized package versions: update `src/Directory.Packages.props` rather than individual csproj package versions.
  - NuGet package management: always use the `dotnet` CLI for adding/updating packages (for example `dotnet add package <Package> --version <Version>`), and update central versions in `src/Directory.Packages.props` when using centrally-managed versions. Do NOT perform repository-wide file-replace edits to bump package versions; avoid manually editing scattered csproj package lines — prefer `dotnet` commands and editing `src/Directory.Packages.props`.
  - Config lookup: the code supports `GitVersion.yml`, `GitVersion.yaml` and dotted variants; use these names or pass explicit `--configfile`.

- **Integration points**:
  - Build agents: see `src/GitVersion.BuildAgents/Agents/*` for platform-specific behavior. Example: GitHub Actions writes `GitVersion_`-prefixed variables to `$GITHUB_ENV` (see [src/GitVersion.BuildAgents/Agents/GitHubActions.cs](src/GitVersion.BuildAgents/Agents/GitHubActions.cs#L1)).
  - Many build-agent adapters write environment variables with a `GitVersion_` prefix — keep that prefix when reading/writing outputs.

- **What to look for when changing behavior**:
  - If you change CLI output shape: update `docs/` examples and adjust build-agent adapters that parse JSON or environment variables.
  - If you add dependencies: update `src/Directory.Packages.props` and validate with the `dotnet` CLI (example below).

- **Quick pointers for the agent**:
  - Prefer editing/adding small focused changes under `src/` and run `dotnet test` on the affected test project(s).
  - Use the `dotnet` CLI to validate packaging and cross-project integration, for example:

    ```bash
    dotnet build ./src/GitVersion.slnx
    dotnet test ./src/GitVersion.slnx
    dotnet format --verify-no-changes ./src/GitVersion.slnx
    ```

If anything here is unclear or you'd like additional examples (e.g., how build agents consume outputs, or a walkthrough to run a specific test), tell me which area to expand.
