---
Order: 25
Title: Telemetry
Description: Telemetry behavior, collected fields, redaction rules, and opt-out for the GitVersion CLI
---

GitVersion can support an optional telemetry pipeline for limited OSS data-collection windows when a telemetry sink is enabled in the distributed build. The purpose is to help maintainers answer product questions such as:

1. Which CLI arguments are used most often
2. Which parser implementation is still in active use
3. Which GitVersion versions are represented in incoming usage data
4. Which command-line flows should be prioritized for UX and documentation improvements

## When telemetry is active

Telemetry is only emitted when a concrete telemetry sink is enabled for the GitVersion distribution you are running.

When telemetry is active, GitVersion shows a one-time disclosure notice before the first telemetry-eligible command sends data. The notice is stored locally so it is not shown on every run.

## What GitVersion collects

The telemetry payload is intentionally narrow and command-line focused. It is designed to support usage analysis, not repository inspection.

Each payload includes:

| Field | Description | Reason |
| --- | --- | --- |
| `toolVersion` | The GitVersion CLI version | Helps correlate behavior with a released version |
| `parserImplementation` | `ArgumentParser` or `LegacyArgumentParser` | Helps understand adoption of the legacy parser |
| `command` | The CLI command name (`gitversion`) | Identifies the invoked entry point |
| `subcommand` | Reserved for future CLI command trees; currently `null` in the stable CLI | Keeps the payload shape stable |
| `arguments[].name` | Canonical argument name | Shows which switches/options are used |
| `arguments[].values` | Argument values when allowed | Shows which non-sensitive modes and options are used |

## Argument value rules

GitVersion keeps argument names but applies redaction rules to values when they may contain file-system or sensitive information.

### Collected as plain values

Examples include:

- `output`
- `show-variable`
- `format`
- `verbosity`
- `branch`
- `commit`
- `override-config`

### Redacted as path values

These arguments keep their names, but their values are replaced with `<redacted:path>`:

- `path`
- `target-path`
- `log-file`
- `config`
- `dynamic-repo-location`
- `update-assembly-info`
- `update-project-files`

For path-oriented switches that accept boolean forms, explicit boolean values such as `true`, `false`, `1`, or `0` are kept. Non-boolean path values are redacted.

### Redacted as sensitive values

These arguments keep their names, but their values are replaced with `<redacted:sensitive>`:

- `url`
- `username`
- `password`

## What GitVersion does not collect

GitVersion telemetry is not intended to inspect repositories or build artifacts. In particular, it does not collect:

- repository contents
- commit messages
- branch history
- configuration file contents
- generated version variables
- file contents
- raw path values
- raw credential values

## Opting out

You can disable telemetry in any of these ways:

1. Set `DO_NOT_TRACK=1` or `DO_NOT_TRACK=true`
2. Set `GITVERSION_TELEMETRY_OPTOUT=1` or `GITVERSION_TELEMETRY_OPTOUT=true`
3. Pass `--telemetry-opt-out` for a single invocation

If any of these opt-out mechanisms are used, GitVersion treats telemetry as disabled for that invocation.

## Collection windows

Telemetry is intended for time-boxed collection windows to support OSS design decisions. A distribution can enable telemetry for a limited period, disable it, and later enable it again for a new decision-making window.
