---
Order: 50
Title: Troubleshooting
RedirectFrom: docs/faq
---
Start by recording the installed GitVersion version, the commit SHA, the effective configuration, and the output you expected. Compare the same commit and configuration locally and in CI.

## Why is my version not incrementing?

A new CI run is not necessarily a new version. Check the [deployment mode](/docs/reference/modes), branch increment settings, applicable tags, and [commit-message increments](/docs/reference/version-increments).

Use `dotnet-gitversion --show-config` to inspect the effective configuration. Changing the output format will not change the underlying version calculation.

## Local and CI versions differ

Check whether both environments have the same commit, branch context, configuration file, tags, and history. A shallow checkout or missing remote branch can affect the available version sources.

Follow the [CI checkout guide](/docs/reference/build-servers), then the page for your provider.

<a id="how-can-gitversion-run-for-a-shallow-clone-or-checkout-on-server-working-directories"></a>

## Shallow clone or missing history

Use a full checkout with the required tags and branches. For an existing shallow clone, fetch the missing history before running GitVersion. See [repository requirements](/docs/reference/requirements).

The `--allow-shallow` argument permits running on a shallow clone; it does not restore missing history or guarantee that the result matches a full clone.

If no suitable local checkout is available, consider [dynamic repositories](/docs/learn/dynamic-repositories).

## Branch detection and detached HEAD

Ensure that the provider supplies the intended branch or tag and that the corresponding references are available. Review [repository setup](/docs/learn/git-setup) and provider-specific [CI guidance](/docs/reference/build-servers).

## Configuration is not being used

Check that the configuration file is checked out. Recognized names include `GitVersion.yml`, `GitVersion.yaml`, `.GitVersion.yml`, and `.GitVersion.yaml`.

Select it explicitly and inspect the result:

```shell
dotnet-gitversion --config GitVersion.yml --show-config
```

See [Configure GitVersion](/docs/usage/configure).

<a id="i-cant-use-the-build-number-for-nuget"></a>

## Package or assembly version is unsuitable

Choose a variable supported by the consumer. Start with `SemVer` for the semantic version and the assembly-specific variables for .NET assemblies. Consult the [variable reference](/docs/reference/variables) and [custom formatting](/docs/reference/custom-formatting).

Old examples mentioning `NuGetVersion` or `NuGetVersionV2` do not describe the current output-variable set.

## Merged branch names as version source

A deleted branch name is not durable history. Version tags and merge messages can retain information that a deleted branch reference cannot. See [calculation strategies](/docs/reference/version-sources) and the [workflow examples](/docs/learn/branching-strategies).

## Collect diagnostic information

```shell
dotnet-gitversion --show-config
dotnet-gitversion --verbosity Diagnostic --log-file gitversion.log
```

To bypass cached calculation during an investigation, use `--no-cache`. Review logs and configuration for credentials and private repository details before sharing them in an issue.

For additional graph diagnostics, `--diagnose` requires `--log-file` and Git installed. See [CLI arguments](/docs/usage/cli/arguments).

<a id="im-using-octopus-deploy"></a>
<a id="i-dont-understand-what-semver-is-all-about"></a>

## More help

- [Upgrade scripts from v6 to v7](/docs/migration/v6-to-v7).
- [Introduction to semantic versioning](/docs/learn/intro-to-semver).
- [Version packages for Octopus Deploy](/docs/reference/build-servers/octopus-deploy).
- [Ask a question](https://github.com/GitTools/GitVersion/discussions).
