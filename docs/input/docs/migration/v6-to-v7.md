---
Order: 10
Title: Migration v6 to v7
Description: Migration guidance for upgrading from GitVersion v6 to GitVersion v7.
---

This document summarizes the relevant breaking changes when migrating from GitVersion v6 to v7.

## CLI Arguments - POSIX-style syntax

GitVersion now uses POSIX-style command-line arguments powered by System.CommandLine.

:::{.alert .alert-warning}
**Breaking change:** Legacy Windows-style (`/switch`) and legacy single-dash long-form (`-switch`) arguments are no longer accepted by default.

As a temporary migration aid, set `GITVERSION_USE_V6_ARGUMENT_PARSER=true` to restore legacy argument handling. This compatibility mode is temporary and will be removed in a future release.
:::

### What you need to change

1. Replace old argument names with POSIX-style `--long-name` arguments.
2. Prefer `--long-name` arguments for readability and maintainability. Supported short aliases (`-o`, `-v`, `-f`, `-c`, `-l`, `-d`, `-b`, `-u`, `-p`) remain available for interactive use.
3. Update scripts that used `-c <commit>` to `--commit <commit>`.
4. Optionally use `GITVERSION_REMOTE_USERNAME` and `GITVERSION_REMOTE_PASSWORD` instead of passing credentials on the command line.

### Full argument mapping

| Old argument                  | New argument                     | Short alias                    | Env var alternative          |
| ----------------------------- | -------------------------------- | ------------------------------ | ---------------------------- |
| `/targetpath <path>`          | `--target-path <path>`           | _(positional `path` argument)_ |                              |
| `/output <type>`              | `--output <type>`                | `-o`                           |                              |
| `/outputfile <path>`          | `--output-file <path>`           |                                |                              |
| `/showvariable <var>`         | `--show-variable <var>`          | `-v`                           |                              |
| `/format <format>`            | `--format <format>`              | `-f`                           |                              |
| `/config <path>`              | `--config <path>`                | `-c`                           |                              |
| `/showconfig`                 | `--show-config`                  |                                |                              |
| `/overrideconfig <k=v>`       | `--override-config <k=v>`        |                                |                              |
| `/nocache`                    | `--no-cache`                     |                                |                              |
| `/nofetch`                    | `--no-fetch`                     |                                |                              |
| `/nonormalize`                | `--no-normalize`                 |                                |                              |
| `/allowshallow`               | `--allow-shallow`                |                                |                              |
| `/verbosity <level>`          | `--verbosity <level>`            |                                |                              |
| `/l <path>`                   | `--log-file <path>`              | `-l`                           |                              |
| `/diag`                       | `--diagnose`                     | `-d`                           |                              |
| `/updateassemblyinfo [files]` | `--update-assembly-info [files]` |                                |                              |
| `/updateprojectfiles [files]` | `--update-project-files [files]` |                                |                              |
| `/ensureassemblyinfo`         | `--ensure-assembly-info`         |                                |                              |
| `/updatewixversionfile`       | `--update-wix-version-file`      |                                |                              |
| `/url <url>`                  | `--url <url>`                    |                                |                              |
| `/b <branch>`                 | `--branch <branch>`              | `-b`                           |                              |
| `/u <username>`               | `--username <username>`          | `-u`                           | `GITVERSION_REMOTE_USERNAME` |
| `/p <password>`               | `--password <password>`          | `-p`                           | `GITVERSION_REMOTE_PASSWORD` |
| `/c <commit>`                 | `--commit <commit>`              | _(no short alias)_             |                              |
| `/dynamicRepoLocation <path>` | `--dynamic-repo-location <path>` |                                |                              |

:::{.alert .alert-danger}
**Important:** `-c` now maps to `--config`.

If you previously used `-c <commit-id>`, you must replace it with `--commit <commit-id>`.
:::

### Example updates

```bash
# Before
gitversion /output json /showvariable SemVer /config GitVersion.yml

# After
gitversion --output json --show-variable SemVer --config GitVersion.yml
```

```bash
# Before
gitversion /url https://github.com/org/repo.git /b main /u user /p pass /c a1b2c3

# After
gitversion --url https://github.com/org/repo.git --branch main --username user --password pass --commit a1b2c3
```

For current command details and examples, see [CLI Arguments](/docs/usage/cli/arguments).

## Git backend

GitVersion v7 introduces a fully managed Git backend as an alternative to the native LibGit2Sharp (libgit2) implementation. The backend is selected with the `GITVERSION_GIT_BACKEND` environment variable.

:::{.alert .alert-info}
In v7.0 the `libgit2` backend remains the **default** — behaviour is unchanged unless you opt in. Set `GITVERSION_GIT_BACKEND=managed` to try the managed backend and help validate it. In v7.1 the default flips to `managed`, with `GITVERSION_GIT_BACKEND=libgit2` available as a fallback. Both backends ship side by side for several releases before libgit2 is removed.
:::

- `libgit2` — the native, libgit2-based backend (default in v7.0).
- `managed` — a managed implementation for all read/history operations, combined with the `git` command-line executable for network and write operations (clone, fetch, checkout, and CI repository normalization).

:::{.alert .alert-warning}
When using the `managed` backend, the `git` executable must be available on the `PATH` **only** for the network/normalization scenarios above (dynamic repositories, build-agent normalization). Plain version calculation on an already-prepared checkout does not require `git` on the `PATH`.
:::

## Environment variables

The environment variables relevant to migrating from v6 to v7:

| Variable                            | Purpose                                                                                                             |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| `GITVERSION_GIT_BACKEND`            | Selects the Git backend: `libgit2` (default in v7.0) or `managed`. See [Git backend](#git-backend).                 |
| `GITVERSION_USE_V6_ARGUMENT_PARSER` | Set to `true` to temporarily restore the legacy v6 (`/switch`) argument parser. Removed in a future release.        |
| `GITVERSION_REMOTE_USERNAME`        | Alternative to `--username` for dynamic-repository credentials.                                                     |
| `GITVERSION_REMOTE_PASSWORD`        | Alternative to `--password` for dynamic-repository credentials.                                                     |
