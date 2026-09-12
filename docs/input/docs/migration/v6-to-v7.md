---
Order: 10
Title: Migration v6 to v7
Description: Migration guidance for upgrading from GitVersion v6 to GitVersion v7.
---

This document summarizes the relevant breaking changes when migrating from GitVersion v6 to v7.

## Pre-release output variables renamed

The pre-release output variables now use SemVer terminology consistently. Update JSON consumers, `--show-variable` arguments, custom format strings, build-agent environment variables, generated version-information files, and `GitVersion.MsBuild` properties according to this mapping:

| GitVersion v6 | GitVersion v7 | Example value |
| --- | --- | --- |
| `PreReleaseTag` | `PreReleaseLabel` | `beta.99` |
| `PreReleaseTagWithDash` | `PreReleaseLabelWithDash` | `-beta.99` |
| `PreReleaseLabel` | `PreReleaseLabelName` | `beta` |
| `PreReleaseLabelWithDash` | `PreReleaseLabelNameWithDash` | `-beta` |

`PreReleaseNumber` is unchanged. In MSBuild and build-agent environments, apply the same mapping to the `GitVersion_`-prefixed names. For example, `GitVersion_PreReleaseTag` becomes `GitVersion_PreReleaseLabel`.

:::{.alert .alert-warning}
`PreReleaseLabel` and `PreReleaseLabelWithDash` retain their names but change from the label name (`beta`) to the full label (`beta.99`). Consumers that require only the name must use `PreReleaseLabelName` and `PreReleaseLabelNameWithDash`.
:::

## Intel macOS artifacts removed

GitVersion v7 no longer ships native `osx-x64` artifacts. Apple Silicon (`osx-arm64`) is now the only supported macOS target. Intel Mac users should continue using the last GitVersion v6 release that shipped an `osx-x64` artifact.

## .NET 8 and .NET 9 target frameworks removed

GitVersion v7 targets .NET 10 only. Install the .NET 10 runtime to use the CLI, global tool, or `GitVersion.MsBuild`. The MSBuild integration can still run in projects targeting earlier frameworks through its `dotnet exec --roll-forward Major` launcher, provided .NET 10 is installed.

## RID-specific global-tool packages

GitVersion v7 packages `GitVersion.Tool` per runtime identifier. Install it with
the .NET 10 SDK as before; NuGet automatically selects the dedicated package for
Windows x64 and ARM64, Linux x64 and ARM64 (including musl), or Apple Silicon
macOS.

## `CommitsSinceVersionSource` output variable removed

`CommitsSinceVersionSource` is no longer emitted in JSON output, build-agent environment variables, generated version-information files, or the MSBuild `GetVersion` task. It can no longer be used in custom format strings.

Use `VersionSourceDistance` instead; it has the same value.

## Separate semantic and counting sources

Five additive output variables distinguish semantic provenance from counting: `SemVerSourceSemVer`, `SemVerSourceSha`, `SemVerSourceIncrement`, `CommitCountSourceSha`, and `CommitCountSourceDistance`. They are available in JSON, custom formats, generated version-information files, build-server output, and MSBuild.

Existing `VersionSource*` fields keep their values. `VersionSourceSha` identifies the counting anchor, which may not be the artifact supplying `VersionSourceSemVer`. The legacy `VersionSourceIncrement` can be `None` after candidate resolution even when an increment was applied; new integrations should use `SemVerSourceIncrement`.

For the removed v6 `CommitsSinceVersionSource`, `VersionSourceDistance` remains a compatible replacement; `CommitCountSourceDistance` is the explicit name for the same count. Do not replace that count with a distance from `SemVerSourceSha`: configuration and branch-name sources have no intrinsic SHA. New nullable source fields use JSON null; textual outputs use an empty string.

Update strict JSON property-set consumers for the five added fields. Public source interfaces and existing constructors remain available. This change does not alter candidate ordering, version numbers, or commit counts. See [version variables](/docs/reference/variables) for a concrete example.

## CLI Arguments - POSIX-style syntax

GitVersion now uses POSIX-style command-line arguments powered by System.CommandLine.

:::{.alert .alert-warning}
**Breaking change:** Legacy Windows-style (`/switch`) and legacy single-dash long-form (`-switch`) arguments are no longer accepted by default.

As a temporary v7.0 migration aid, set `GITVERSION_ARGUMENT_PARSER_VERSION=v6` to restore legacy argument handling. The legacy parser is removed in v7.1. Unset the retired `GITVERSION_USE_V6_ARGUMENT_PARSER` variable: its presence is an error, even when set to `false`; it is not an alias for the new selector.
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

## Configuration migration

GitVersion v7 stores settings under `calculation` and `output`. Convert an
existing flat v6 YAML document with the migration command using the default
POSIX-style argument parser. This works on all supported operating systems;
the legacy v6 argument parser does not support the command:

```shell
gitversion config migrate
gitversion config migrate --config GitVersion.yml --output GitVersion.v7.yml
gitversion config migrate --config GitVersion.yml --in-place
```

The first command discovers `GitVersion.yml` and emits v7 YAML to stdout.
`--output` requires `--force` to replace an existing file and cannot be used
with `--in-place`. In-place migration warns because comments cannot be
preserved. The command does not need a Git repository and migrating its v7
output again is idempotent. In v7.0, you can temporarily validate a flat file
with `GITVERSION_CONFIGURATION_VERSION=v6`; GitVersion warns once for that
fallback.

GitVersion 7 publishes a nested-only schema at
`https://gitversion.net/schemas/7.0/GitVersion.configuration.json`. If you
temporarily select `GITVERSION_CONFIGURATION_VERSION=v6`, keep the existing v6
`$schema` reference (for example,
`https://gitversion.net/schemas/6.3/GitVersion.configuration.json`) until you
migrate. Do not point a flat document at the 7.0 schema. New v7 configuration
settings require migrating to the nested layout with `gitversion config
migrate` to retain schema validation.

## Git backend

GitVersion v7.0 uses the managed Git backend by default. The `GITVERSION_GIT_BACKEND` environment variable accepts `managed` or the temporary `libgit2` fallback.

:::{.alert .alert-info}
In v7.0, use `GITVERSION_GIT_BACKEND=libgit2` only if you need the temporary native backend fallback. LibGit2Sharp and its native binaries are scheduled for removal in v7.1. Explicit `managed` remains accepted throughout v7.x; the selector is removed in v8.
:::

- `libgit2` — the temporary native backend fallback in v7.0.
- `managed` — the v7.0 default: a managed implementation for all read/history operations, combined with the `git` command-line executable for network and write operations (clone, fetch, checkout, and CI repository normalization).

:::{.alert .alert-warning}
When using the `managed` backend, the `git` executable must be available on the `PATH` **only** for the network/normalization scenarios above (dynamic repositories, build-agent normalization). Plain version calculation on an already-prepared checkout does not require `git` on the `PATH`.
:::

## Environment variables

The environment variables relevant to migrating from v6 to v7:

| Variable                            | Purpose                                                                                                             |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| `GITVERSION_CONFIGURATION_VERSION`   | Selects the configuration layout: `v7` (default) or temporary flat `v6` fallback.                                   |
| `GITVERSION_GIT_BACKEND`             | Selects the Git backend: `managed` (default) or temporary `libgit2` fallback. See [Git backend](#git-backend). |
| `GITVERSION_ARGUMENT_PARSER_VERSION` | Selects the argument parser: `v7` (default, POSIX syntax) or temporary `v6` fallback (`/switch` and `-switch`). |
| `GITVERSION_REMOTE_USERNAME`        | Alternative to `--username` for dynamic-repository credentials.                                                     |
| `GITVERSION_REMOTE_PASSWORD`        | Alternative to `--password` for dynamic-repository credentials.                                                     |

The three selectors are independent: selecting the v6 parser does not select
flat configuration or libgit2. Values are trimmed and case-insensitive;
unset, empty, and whitespace-only values use the defaults. Unknown values
fail before execution and list the accepted values. Replace
`GITVERSION_USE_V6_ARGUMENT_PARSER=true` with
`GITVERSION_ARGUMENT_PARSER_VERSION=v6` and unset the old variable.

Effective selections are logged at information level. Use `--log-file <path>`
to capture them or `--log-file console` to see them on stderr. Console logs
also use stderr when build-server output is selected, including combinations
with JSON or other machine-readable output. Build-server integration commands
retain their normal output channel. For migration, place logging options before the command:
`gitversion --log-file console config migrate --config GitVersion.yml`.

In v7.1, legacy parser and flat configuration runtime support are scheduled
for removal alongside libgit2. Retired `v6`/`libgit2` selections will report
actionable errors; explicit `v7`/`managed` selections remain accepted during
v7.x. All three selectors are removed in v8. `gitversion config migrate`
remains available to convert flat files after runtime support is removed.
