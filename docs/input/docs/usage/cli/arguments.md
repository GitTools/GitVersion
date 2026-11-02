---
Order: 20
Title: Arguments
Description: The supported arguments of the GitVersion Command Line Interface
---

:::{.alert .alert-info}
**Note:** GitVersion uses POSIX-style `--long-name` arguments from version 7 and up. Long-form
arguments are recommended for readability in scripts and documentation. Short aliases
(e.g. `-l`, `-o`, `-b`) are also supported. The legacy `/switch` and `-switch`
syntax is available during v7.0 when `GITVERSION_ARGUMENT_PARSER_VERSION=v6` is set.
The default is `v7`. Unset the retired `GITVERSION_USE_V6_ARGUMENT_PARSER`
variable; it now produces a replacement diagnostic. The legacy parser is
removed in v7.1 and the selector in v8.

See [Migration v6 to v7][migration-v6-to-v7] for upgrade guidance and the full argument mapping.
:::

## Help

Optional POSIX CLI usage telemetry can be disabled with `--telemetry-opt-out`,
`DO_NOT_TRACK=1`, or `GITVERSION_TELEMETRY_OPTOUT=true`. See [Telemetry][telemetry] for collection scope,
sampling, privacy and the fixed campaign window.
The first eligible direct run with a positive direct sampling probability, or the first selected automated
run, links to the [Telemetry policy and collected data][telemetry-policy] on stderr.

Below is the output from `gitversion --help` as a best effort to provide
documentation for which arguments GitVersion supports and their meaning.

```bash
Use convention to derive a SemVer product version from a GitFlow or GitHub based
repository.

GitVersion [path]

    path            The directory containing .git. If not defined current
                    directory is used. (Must be first argument)
    --version       Displays the version of GitVersion
    --diagnose, -d  Runs GitVersion with additional diagnostic information;
                    also needs the '--log-file' argument to specify a logfile
                    or stdout (requires git.exe to be installed)
    --help, -h      Shows Help
    --cli-schema    Writes the complete OpenCLI 0.1 command schema as JSON and exits.

    --target-path   Same as 'path', but not positional
    --output, -o    Determines the output to the console. Can be either 'json',
                    'file', 'buildserver' or 'dotenv', will default to 'json'.
    --output-file   Path to output file. It is used in combination with
                    --output 'file'.
    --show-variable, -v
                    Used in conjunction with --output json, will output just a
                    particular variable.
                    E.g. --output json --show-variable SemVer
                    - will output `1.2.3+beta.4`
    --format, -f    Used in conjunction with --output json, will output a format
                    containing version variables.
                    Supports C# format strings - see [Format Strings](/docs/reference/custom-formatting) for details.
                    E.g. --output json --format {SemVer} - will output `1.2.3+beta.4`
                         --output json --format {Major}.{Minor} - will output `1.2`
    --log-file, -l  Path to logfile; specify 'console' to emit to stderr.
    --config, -c    Path to config file (defaults to GitVersion.yml, GitVersion.yaml, .GitVersion.yml or .GitVersion.yaml)
    --show-config   Outputs the effective GitVersion config (defaults + custom
                    from GitVersion.yml, GitVersion.yaml, .GitVersion.yml or .GitVersion.yaml) in yaml format.
                    With v7 configuration, runtime overrides are also included.
    --override-config
                    Overrides GitVersion config values inline (key=value pairs,
                    e.g. --override-config workflow=GitHubFlow/v1).
                    Repeat --override-config for multiple overrides.
    --no-cache      Bypasses the cache, result will not be written to the cache.
    --no-normalize  Disables normalize step on a build server.
    --allow-shallow Allows GitVersion to run on a shallow clone.
                    This is not recommended, but can be used if you are sure
                    that the shallow clone contains all the information needed
                    to calculate the version.
    --verbosity     Specifies the amount of information to be displayed.
                    (Quiet, Minimal, Normal, Verbose, Diagnostic)
                    Default is Normal

# AssemblyInfo updating

    --update-assembly-info
                    Will recursively search for all 'AssemblyInfo.cs' files in
                    the git repo and update them
    --update-project-files
                    Will recursively search for all project files
                    (.csproj/.vbproj/.fsproj/.sqlproj) files in the git repo and update
                    them
                    Note: This is only compatible with the newer Sdk projects
    --ensure-assembly-info
                    If the assembly info file specified with
                    --update-assembly-info is not
                    found, it will be created with these attributes:
                    AssemblyFileVersion, AssemblyVersion and
                    AssemblyInformationalVersion.
                    Supports writing version info for: C#, F#, VB

# Create or update Wix version file

    --update-wix-version-file
                   All the GitVersion variables are written to
                   'GitVersion_WixVersion.wxi'. The variables can then be
                   referenced in other WiX project files for versioning.

# Remote repository args

    --url           Url to remote git repository.
    --branch, -b    Target branch to version (local or with --url). Takes
                    precedence over GIT_BRANCH/Git_Branch, which only supply
                    branch context at HEAD.
    --username, -u  Username in case authentication is required.
    --password, -p  Password in case authentication is required.
    --commit        The commit id to check. If not specified, the latest
                    available commit on the specified branch will be used.
    --dynamic-repo-location
                    By default dynamic repositories will be cloned to %tmp%.
                    Use this switch to override
    --no-fetch      Disables 'git fetch' during version calculation. Might cause
                    GitVersion to not calculate your version as expected.
```

## Validate configuration

Validate a candidate with the established v7 POSIX CLI before inspecting effective settings:

```shell
gitversion config validate --config GitVersion.yml
gitversion config validate --target-path ./candidate-directory
gitversion config validate --config "candidate configs/équipe.yml" --format json
gitversion config validate --config GitVersion.yml --override-config calculation.tag-prefix='[vV]?'
```

An explicit file works outside Git and offline. Without `--config`, discovery checks
the selected directory and, inside Git, the repository root. Files in both directories
are ambiguous. Within a directory, existing priority is `GitVersion.yml`,
`GitVersion.yaml`, `.GitVersion.yml`, then `.GitVersion.yaml`.
Case variants at the same priority use ordinal path order.
Missing/unreadable files fail; validation never substitutes defaults for a missing
file. An existing empty document resolves workflow defaults.

Validation shares normal execution's deserializer, layout mapper, workflow defaults,
override precedence and builder. Both paths reject unknown/misplaced public keys,
wrong types or enum names, invalid workflows, invalid configured regexes and
references to unconfigured branch definitions. Regex checks include ignore paths,
branches and tags, tag/branch version patterns, bump messages and merge-message
formats. Diagnostics omit configuration values and report the first error.

`next-version` must parse with the selected `semantic-version-format` (`Strict`
requires a complete version such as `2.0.0`; `Loose` permits partial versions).
Commit cutoff dates are checked without inspecting commits.

The command never calculates a version, constructs a Git backend, fetches, normalizes
refs, writes caches, migrates configuration or emits build-server/file-update
outputs. It accepts `--config`, `--override-config`, `--target-path` and its local
`--format json`. A root positional directory must precede `config validate`.
Calculation, remote, file-update and logging options are rejected, including when
placed before the command. Help and `--cli-schema` remain informational operations.

By default, the summary and diagnostics go to stderr and stdout is empty. Exit 0
means the resolved configuration is valid; command-executed invocation,
configuration and file-read errors return 1. Warnings do not fail validation;
there is no strict-warning flag in this initial command.

`--format json` writes exactly one JSON document to stdout, followed by a newline.
The human summary and diagnostics remain on stderr:

```json
{
  "schemaVersion": 1,
  "valid": true,
  "configurationVersion": "v7",
  "file": "/workspace/GitVersion.yml",
  "diagnostics": []
}
```

`file` is the selected absolute path, or null when selection failed.
Each diagnostic contains `code`, `severity` (`error` or `warning`), `path` (public
property path, or empty for a document/invocation error), `message`, `line` and
`column`. Locations are one-based when provided by YAML parsing, otherwise null.
Codes are stable within schema version 1; human messages may change:

| Code | Meaning |
| --- | --- |
| `GVCONFIG_FILE` | Missing or unreadable configuration file |
| `GVCONFIG_DISCOVERY` | Ambiguous discovery |
| `GVCONFIG_INVOCATION` | Incompatible command option or output format |
| `GVCONFIG_OVERRIDE` | Invalid override syntax or unsupported override path |
| `GVCONFIG_YAML` | YAML syntax or deserialization error |
| `GVCONFIG_LAYOUT` | Wrong layout or section ownership |
| `GVCONFIG_PROPERTY` | Unknown key, invalid type or enum |
| `GVCONFIG_WORKFLOW` | Unknown workflow |
| `GVCONFIG_REGEX` | Invalid regular expression |
| `GVCONFIG_BRANCH` | Missing branch regex or unresolved branch reference |
| `GVCONFIG_VALUE` | Invalid semantic value or invocation path |
| `GVCONFIG_V6_COMPATIBILITY` | Non-failing temporary v6 compatibility warning |

Parser failures before command execution (unknown options, missing option arguments,
extra positional arguments) retain ordinary stderr/exit 1 behavior without JSON.
Feature-selector errors also occur before command execution.

Configuration layout selection remains separate from parser selection. Explicit
`GITVERSION_CONFIGURATION_VERSION=v6` accepts flat files during v7.0 and emits the
existing warning; legacy loading is removed in v7.1. Invalid v7 documents never
fall back to v6. Use `config migrate` to convert a legacy file. This command is
unavailable with the legacy parser and published v6 tools.

Validation does not establish that versions match your release policy. Inspect
effective configuration next and verify calculations on representative existing
history. A preview workflow, a different branching model, or the absence of matching
branches in history is not itself a validation error.

## Query a configuration property

Use `gitversion config get <property-path>` to read one scalar from the effective
**v7 configuration**. It uses the same configuration discovery, workflow defaults,
file settings and runtime overrides as `--show-config`. It requires a Git
repository, but does not calculate a version or update build-agent outputs or
version files. As with full display, conflicting configuration files in the
working directory and repository root require an explicit `--config` path.

```shell
# Default tag prefix: "[vV]?"
gitversion config get calculation.tag-prefix

# Runtime override: false
gitversion config get output.update-build-number --override-config output.update-build-number=false

# Unknown property: diagnostic on stderr, empty stdout, exit status 1
gitversion config get calculation.no-such-property
```

Paths are case-sensitive, dot-separated public configuration names, such as
`calculation.tag-prefix`, `output.assembly-versioning-scheme`, root `workflow`,
or `calculation.prevent-increment.of-merged-branch`. CLR names and flat v6 paths
are not accepted. Empty segments, escaping, wildcards and array indexing are
not supported. Maps (including branch entries and `merge-message-formats`),
collections and whole objects cannot be queried. A missing map key therefore
produces an unsupported-map diagnostic, not a null value.

Successful stdout contains exactly one JSON scalar followed by a newline:

| Value | Output |
| --- | --- |
| String or enum name | JSON string, e.g. `"MajorMinorPatch"` |
| Empty string | `""` |
| Boolean | `true` or `false` |
| Number | Culture-independent JSON number, e.g. `60000` |
| Known property whose resolved value is null | `null` |

Strings retain their type: a string containing `null` is emitted as `"null"`.
Quotes, newlines and other special characters are JSON-escaped. Version-format
strings are returned literally; placeholders are not evaluated. A known null
property remains queryable even when full YAML display omits it. The query
returns the final resolved value and does not distinguish a default null from
an explicitly configured null.

Success, including null, returns status 0. Unknown properties, invalid paths,
unsupported values and configuration errors return status 1 with a diagnostic
on stderr and no value on stdout. Logging and warnings also use stderr or the
selected log file. These are document settings, not branch-specific inherited
runtime behavior or calculated version variables; `--show-variable` continues
to select calculated version variables.

Supported options are `--config`/`-c`, repeated `--override-config key=value`,
`--target-path`, `--log-file`/`-l` and `--verbosity`. A positional repository path
must precede `config get`. Version-output, calculation and file-update options
cannot be combined with this command. Use `gitversion config get --help` for help.

The command requires the default modern argument parser and default or explicit
`GITVERSION_CONFIGURATION_VERSION=v7`. Selecting v6 configuration produces a
v7-required error before configuration loading, even when the modern parser is
selected; GitVersion does not switch versions implicitly. Existing v6 full
configuration display retains its behavior. With v7 configuration,
`--show-config` now includes runtime overrides, matching the query result.

## CLI schema

`gitversion --cli-schema` exports the command tree in the [OpenCLI format][cli-schema].
It is available with the default v7 argument parser and does not require a Git repository.

## Configuration migration

The default POSIX-style argument parser exposes a `config migrate` subcommand for converting a v6
configuration document to the v7 `calculation`/`output` layout:

```shell
gitversion config migrate
gitversion config migrate --config GitVersion.yml --output GitVersion.v7.yml
gitversion config migrate --config GitVersion.yml --in-place
```

It discovers a supported configuration filename when `--config` is omitted and
writes YAML to stdout unless `--output` or `--in-place` is selected. `--output`
will not replace an existing file without `--force`; it cannot be combined with
`--in-place`. Replacing a file warns that comments are not preserved. The
command does not require a Git repository and is unavailable when
`GITVERSION_ARGUMENT_PARSER_VERSION=v6` selects the legacy parser. Migration
remains available after v6 runtime support is removed in v7.1.

## Override config

`--override-config key=value` will override appropriate `key` from 'GitVersion.yml', 'GitVersion.yaml', '.GitVersion.yml' or '.GitVersion.yaml'.

With the v7 default configuration layout, use a version-aware nested key. For
example, `calculation.tag-prefix=custom`,
`calculation.branches.main.increment=Patch`, and
`output.branches.main.pre-release-weight=55000`. Flat v6 keys are rejected in
v7 mode with their nested replacement. Set
`GITVERSION_CONFIGURATION_VERSION=v6` only while validating a legacy file in
v7.0.

When that temporary v6 fallback is selected, use the legacy branch override
path, for example `--override-config branches.main.increment=Patch`.

To specify multiple options add multiple `--override-config key=value` entries:
`--override-config key1=value1 --override-config key2=value2`.

To have **space characters** as a part of `value`, `value` has to be enclosed with double quotes - `key="My value"`.

Double quote character inside of the double quoted `value` has to be escaped with a backslash '\\' - `key="My \"escaped-quotes\""`.

The following override paths are supported in v7. For the temporary v6
fallback, omit the `calculation.` or `output.` prefix. The shared `workflow`
key stays at the root in both versions:

1. `output.assembly-file-versioning-format`
2. `output.assembly-file-versioning-scheme`
3. `output.assembly-informational-format`
4. `output.assembly-versioning-format`
5. `output.assembly-versioning-scheme`
6. `output.commit-date-format`
7. `calculation.commit-message-incrementing`
8. `output.custom-version-format`
9. `calculation.label`
10. `calculation.increment`
11. `calculation.major-version-bump-message`
12. `calculation.minor-version-bump-message`
13. `calculation.mode`
14. `calculation.next-version`
15. `calculation.no-bump-message`
16. `calculation.patch-version-bump-message`
17. `calculation.tag-prefix`
18. `output.tag-pre-release-weight`
19. `output.update-build-number`
20. `calculation.version-bump-reset-message`
21. `workflow`

Read more about [Configuration][configuration].

Using `override-config` on the command line will not change the contents of the config file `GitVersion.yml`, `GitVersion.yaml`, `.GitVersion.yml` or `.GitVersion.yaml`.

### Example: How to override configuration option 'tag-prefix' to use prefix 'custom'

`GitVersion.exe --output json --override-config calculation.tag-prefix=custom`

### Example: How to override configuration option 'assembly-versioning-format'

`GitVersion.exe --output json --override-config output.assembly-versioning-format="{Major}.{Minor}.{Patch}.{env:BUILD_NUMBER ?? 0}"`

Uses the environment variable `BUILD_NUMBER`, or falls back to zero for the assembly revision number.

### Example: How to override configuration option 'assembly-versioning-scheme'

`GitVersion.exe --output json --override-config output.assembly-versioning-scheme=MajorMinor`

Will use only major and minor version numbers for assembly version. Assembly build and revision numbers will be 0 (e.g. `1.2.0.0`)

### Example: How to override multiple configuration options

`GitVersion.exe --output json --override-config calculation.tag-prefix=custom --override-config output.assembly-versioning-scheme=MajorMinor`

### Example: How to override configuration option 'update-build-number'

`GitVersion.exe --output json --override-config output.update-build-number=true`

### Example: How to override configuration option 'next-version'

`GitVersion.exe --output json --override-config calculation.next-version=6`

[migration-v6-to-v7]: /docs/migration/v6-to-v7

[configuration]: /docs/reference/configuration

[cli-schema]: /docs/usage/cli/cli-schema

[telemetry]: /docs/usage/cli/telemetry

[telemetry-policy]: /docs/usage/cli/telemetry-policy
