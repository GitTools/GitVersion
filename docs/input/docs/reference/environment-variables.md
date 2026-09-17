---
Title: Environment variables
Order: 60
---
Distinguish variables **read by GitVersion** from version variables **exported to your build**.

## Repository and execution inputs

`GIT_BRANCH` supplies branch context for the checked-out commit, including detached
HEAD and pull-request merge checkouts. `Git_Branch` is an alias on every platform.
The precedence is explicit CLI `--branch` or API `RepositoryInfo.TargetBranch`, then
nonblank `GIT_BRANCH`, then nonblank `Git_Branch`, then the provider's branch ref,
then HEAD. On case-sensitive platforms, `GIT_BRANCH` wins an alias conflict.
Empty and whitespace-only values are absent.

An environment override behaves like a branch at the checked-out commit: its name
selects branch configuration and appears in version outputs, while its history is
the history reachable from that commit. No physical branch is required. If a branch
with that name already points elsewhere, its ref and the checkout remain unchanged.
Cache entries include this context. The override also works with `--no-normalize`;
when normalization is enabled it can fetch history, but does not rewrite branches
or attach HEAD for the override.

For a contextual pull-request branch whose merge message identifies the target,
a successful preparation fetch makes the matching remote-tracking target take
precedence over a stale local target. Local refs remain unchanged. When preparation
does not fetch (including `--no-fetch`, `--no-normalize`, local builds, or a provider
that disables fetching), target selection retains its normal local preference.

Use a valid Git branch name such as `feature/work`. `refs/heads/feature/work` is
accepted as `feature/work`; `refs/remotes/origin/feature/work` also supplies
`feature/work` context. A short name such as `origin/feature/work` is literal.
Nonblank names are not trimmed. Invalid names, tag refs and pull-request refs are
rejected. To label a PR merge checkout, supply the intended branch name, for example
`GIT_BRANCH=feature/work`. Provider tag detection remains separate; do not use this
variable to select a tag or commit.

Explicit branch and commit options retain their target-selection semantics;
`--branch` can select another branch's history, and an explicit `--commit` keeps
the existing history/time-based selection behavior. For a dynamic repository, an
environment override uses the freshly cloned repository's default HEAD, or the
reused clone's HEAD. Use `--branch` to select a remote target instead.

GitVersion reads the configuration file from the working/project directory as
usual; an environment branch override does not check out another configuration file.
Provider-specific detection is described in the [CI guides](/docs/reference/build-servers).

The [v6 to v7 migration guide](/docs/migration/v6-to-v7#environment-variables) documents the independent selectors `GITVERSION_ARGUMENT_PARSER_VERSION`, `GITVERSION_CONFIGURATION_VERSION`, and `GITVERSION_GIT_BACKEND`. Their v7.0 defaults are `v7`, `v7`, and `managed`; temporary fallbacks are `v6`, `v6`, and `libgit2`. Values are trimmed and case-insensitive; blank values use the defaults and unknown values fail with accepted-value guidance. The retired `GITVERSION_USE_V6_ARGUMENT_PARSER` variable must be unset. Legacy implementations are removed in v7.1 and selectors in v8.

## Output variables

With a supported build-server integration, version variables are made available using the provider's mechanisms. A common environment-variable spelling is `GitVersion_SemVer`. Use the exact naming and job/step scope described for your provider.

A child process cannot generally set environment variables in its parent shell. For shell scripts, use [JSON or dotenv output](/docs/usage/cli/output) and explicitly pass the result to later commands.

For .NET builds, see the [MSBuild integration](/docs/usage/msbuild). For every available value and its meaning, see [version variables](/docs/reference/variables).
