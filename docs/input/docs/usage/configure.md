---
Title: Configure GitVersion
Order: 2
---
Start with a workflow and override only the settings your repository needs.

## Create a configuration file

Put `GitVersion.yml` at the repository root. GitVersion also recognizes `GitVersion.yaml`, `.GitVersion.yml`, and `.GitVersion.yaml`.

```yaml
workflow: GitHubFlow/v1
tag-prefix: '[vV]?'
```

This selects the GitHubFlow defaults and permits a leading v or V in version tags. For a different starting point, [choose a workflow](/docs/usage/choose-workflow).

## Inspect the effective configuration

```shell
dotnet-gitversion --show-config
```

The output includes defaults and your overrides. If you work in a subdirectory or maintain multiple configuration files, select the intended file explicitly:

```shell
dotnet-gitversion --config GitVersion.yml --show-config
dotnet-gitversion --config GitVersion.yml --show-variable SemVer
```

Check that the file is included in your CI checkout. See [configuration troubleshooting](/docs/learn/faq#configuration-is-not-being-used).

## Change one responsibility at a time

- **Calculation:** tags, version sources, increments, and ignored history.
- **Branches:** matching rules, inheritance, deployment mode, and pre-release labels.
- **Output:** assembly versions, custom formatting, and CI build-number updates.

Labels affect the calculated semantic version; they are not only a display preference.

Use [configuration by topic](/docs/reference/configuration-topics) to find the relevant settings. Before adopting a change, compare the effective configuration and output on histories that represent your release process.
