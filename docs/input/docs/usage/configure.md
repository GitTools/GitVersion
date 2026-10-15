---
Title: Configure GitVersion
Order: 2
---
Start with a workflow and override only the settings your repository needs.

Use [AI-agent onboarding][ai-agent-onboarding] for a guided v7 setup that compares candidate settings against your existing branches, tags and commits.

The old interactive `init` configuration tool was [removed in v6][removed-in-v6]. Create and edit the YAML file directly.

## Create a configuration file

Put `GitVersion.yml` at the repository root. GitVersion also recognizes `GitVersion.yaml`, `.GitVersion.yml`, and `.GitVersion.yaml`.

```yaml
workflow: GitHubFlow/v1
calculation:
  tag-prefix: '[vV]?'
```

This selects the GitHubFlow defaults and permits a leading v or V in version tags. For a different starting point, [choose a workflow][choose-a-workflow].

## Validate the file

With the v7 POSIX CLI, check the candidate before inspecting defaults or calculating versions:

```shell
dotnet-gitversion config validate --config GitVersion.yml
```

An explicit file works outside Git and offline. Exit 0 means the file, workflow
defaults and overrides resolve; errors return 1. Human output goes to stderr.
Add `--format json` for a versioned result on stdout. Warnings do not fail validation.
See [configuration validation][configuration-validation] for diagnostics and limits.

For older tools lacking this command, retain matching-version schema and loader
checks. Successful validation still needs effective-config inspection and
calculations on representative history before adopting a release-policy change.

## Inspect the effective configuration

```shell
dotnet-gitversion --show-config
```

The output includes defaults and your overrides. If you work in a subdirectory or maintain multiple configuration files, select the intended file explicitly:

```shell
dotnet-gitversion --config GitVersion.yml --show-config
dotnet-gitversion --config GitVersion.yml --show-variable SemVer
```

Check that the file is included in your CI checkout. See [configuration troubleshooting][configuration-troubleshooting].

## Change one responsibility at a time

- **Calculation:** tags, version sources, increments, and ignored history.
- **Branches:** matching rules, inheritance, deployment mode, and pre-release labels.
- **Output:** assembly versions, custom formatting, and CI build-number updates.

Labels affect the calculated semantic version; they are not only a display preference.

Use [configuration by topic][configuration-by-topic] to find the relevant settings. Before adopting a change, compare the effective configuration and output on histories that represent your release process.

[removed-in-v6]: /docs/reference/configuration#configuration-tool

[choose-a-workflow]: /docs/usage/choose-workflow

[configuration-troubleshooting]: /docs/learn/faq#configuration-is-not-being-used

[configuration-by-topic]: /docs/reference/configuration-topics

[configuration-validation]: /docs/usage/cli/arguments#validate-configuration

[ai-agent-onboarding]: /docs/usage/ai-agent-onboarding
