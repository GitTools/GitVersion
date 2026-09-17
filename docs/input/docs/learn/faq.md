---
Order: 50
Title: Troubleshooting
RedirectFrom: docs/faq
---
Start by recording the installed GitVersion version, the commit SHA, the effective configuration, and the output you expected. Compare the same commit and configuration locally and in CI.

## Why is my version not incrementing?

A new CI run is not necessarily a new version. Check the [deployment mode][deployment-mode], branch increment settings, applicable tags, and [commit-message increments][commit-message-increments].

Use `dotnet-gitversion --show-config` to inspect the effective configuration. Changing the output format will not change the underlying version calculation.

## Local and CI versions differ

Check whether both environments have the same commit, branch context, configuration file, tags, and history. A shallow checkout or missing remote branch can affect the available version sources.

Follow the [CI checkout guide][ci-checkout-guide], then the page for your provider.

<a id="how-can-gitversion-run-for-a-shallow-clone-or-checkout-on-server-working-directories"></a>

## Shallow clone or missing history

Use a full checkout with the required tags and branches. For an existing shallow clone, fetch the missing history before running GitVersion. See [repository requirements][repository-requirements].

The `--allow-shallow` argument permits running on a shallow clone; it does not restore missing history or guarantee that the result matches a full clone.

If no suitable local checkout is available, consider [dynamic repositories][dynamic-repositories].

## Branch detection and detached HEAD

Ensure that the provider supplies the intended branch or tag and that the corresponding references are available. Review [repository setup][repository-setup] and provider-specific [CI guidance][ci-checkout-guide].

## Configuration is not being used

Check that the configuration file is checked out. Recognized names include `GitVersion.yml`, `GitVersion.yaml`, `.GitVersion.yml`, and `.GitVersion.yaml`.

Select it explicitly and inspect the result:

```shell
dotnet-gitversion --config GitVersion.yml --show-config
```

See [Configure GitVersion][configure-gitversion].

<a id="i-cant-use-the-build-number-for-nuget"></a>

## Package or assembly version is unsuitable

Choose a variable supported by the consumer. Start with `SemVer` for the semantic version and the assembly-specific variables for .NET assemblies. Consult the [variable reference][variable-reference] and [custom formatting][custom-formatting].

Old examples mentioning `NuGetVersion` or `NuGetVersionV2` do not describe the current output-variable set.

## Merged branch names as version source

A deleted branch name is not durable history. Version tags and merge messages can retain information that a deleted branch reference cannot. See [calculation strategies][calculation-strategies] and the [workflow examples][workflow-examples].

## Collect diagnostic information

```shell
dotnet-gitversion --show-config
dotnet-gitversion --verbosity Diagnostic --log-file gitversion.log
```

To bypass cached calculation during an investigation, use `--no-cache`. Review logs and configuration for credentials and private repository details before sharing them in an issue.

For additional graph diagnostics, `--diagnose` requires `--log-file` and Git installed. See [CLI arguments][cli-arguments].

<a id="im-using-octopus-deploy"></a>
<a id="i-dont-understand-what-semver-is-all-about"></a>

## More help

- [Upgrade scripts from v6 to v7][upgrade-scripts-from-v6-to-v7].
- [Introduction to semantic versioning][introduction-to-semantic-versioning].
- [Version packages for Octopus Deploy][version-packages-for-octopus-deploy].
- [Ask a question][ask-a-question].

[deployment-mode]: /docs/reference/modes

[commit-message-increments]: /docs/reference/version-increments

[ci-checkout-guide]: /docs/reference/build-servers

[repository-requirements]: /docs/reference/requirements

[dynamic-repositories]: /docs/learn/dynamic-repositories

[repository-setup]: /docs/learn/git-setup

[configure-gitversion]: /docs/usage/configure

[variable-reference]: /docs/reference/variables

[custom-formatting]: /docs/reference/custom-formatting

[calculation-strategies]: /docs/reference/version-sources

[workflow-examples]: /docs/learn/branching-strategies

[cli-arguments]: /docs/usage/cli/arguments

[upgrade-scripts-from-v6-to-v7]: /docs/migration/v6-to-v7

[introduction-to-semantic-versioning]: /docs/learn/intro-to-semver

[version-packages-for-octopus-deploy]: /docs/reference/build-servers/octopus-deploy

[ask-a-question]: https://github.com/GitTools/GitVersion/discussions
