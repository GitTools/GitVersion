---
Order: 10
Title: Migration v6 to v7
Description: Migration guidance for upgrading from GitVersion v6 to GitVersion v7.
---

This document summarizes the relevant breaking changes when migrating from GitVersion v6 to v7.

## Intel macOS artifacts removed

GitVersion v7 no longer ships native `osx-x64` artifacts. Apple Silicon (`osx-arm64`) is now the only supported macOS target. Intel Mac users should continue using the last GitVersion v6 release that shipped an `osx-x64` artifact.

## .NET 8 and .NET 9 target frameworks removed

GitVersion v7 targets .NET 10 only. Install the .NET 10 runtime to use the CLI, global tool, or `GitVersion.MsBuild`. The MSBuild integration can still run in projects targeting earlier frameworks through its `dotnet --roll-forward Major` launcher, provided .NET 10 is installed.

## `CommitsSinceVersionSource` output variable removed

`CommitsSinceVersionSource` is no longer emitted in JSON output, build-agent environment variables, generated version-information files, or the MSBuild `GetVersion` task. It can no longer be used in custom format strings.

Use `VersionSourceDistance` instead; it has the same value.

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
