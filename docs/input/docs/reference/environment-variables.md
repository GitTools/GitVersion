---
Title: Environment variables
Order: 60
---
Distinguish variables **read by GitVersion** from version variables **exported to your build**.

## Repository and execution inputs

`Git_Branch` can identify the branch or tag when the build context is ambiguous. Provider-specific detection is described in the [CI guides](/docs/reference/build-servers). Ensure the selected reference and its history are available in the checkout.

The [v6 to v7 migration guide](/docs/migration/v6-to-v7#environment-variables) documents the independent selectors `GITVERSION_ARGUMENT_PARSER_VERSION`, `GITVERSION_CONFIGURATION_VERSION`, and `GITVERSION_GIT_BACKEND`. Their v7.0 defaults are `v7`, `v7`, and `managed`; temporary fallbacks are `v6`, `v6`, and `libgit2`. Values are trimmed and case-insensitive; blank values use the defaults and unknown values fail with accepted-value guidance. The retired `GITVERSION_USE_V6_ARGUMENT_PARSER` variable must be unset. Legacy implementations are removed in v7.1 and selectors in v8.

## Output variables

With a supported build-server integration, version variables are made available using the provider's mechanisms. A common environment-variable spelling is `GitVersion_SemVer`. Use the exact naming and job/step scope described for your provider.

A child process cannot generally set environment variables in its parent shell. For shell scripts, use [JSON or dotenv output](/docs/usage/cli/output) and explicitly pass the result to later commands.

For .NET builds, see the [MSBuild integration](/docs/usage/msbuild). For every available value and its meaning, see [version variables](/docs/reference/variables).
