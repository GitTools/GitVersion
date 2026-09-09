---
Order: 40
Title: Trunk-based (preview)
Description: Configure and evaluate the experimental TrunkBased workflow
---

Use `TrunkBased/preview1` when your team integrates into one main branch,
works directly on that branch or uses short-lived feature and hotfix branches,
and wants versions calculated from that mainline history. Release tags record
published versions; a separate develop or release branch is not required.

This preset is **experimental**. Evaluate it against representative histories
before adopting it. Its defaults and behavior may change; it does not have the
same compatibility guarantees as `GitFlow/v1` and `GitHubFlow/v1`.

## Start with the preset

Create `GitVersion.yml` at the repository root:

```yaml
workflow: TrunkBased/preview1
```

Inspect the resolved defaults and the version at your current commit:

```shell
dotnet-gitversion --show-config
dotnet-gitversion --show-variable FullSemVer
```

The preset selects the `ConfiguredNextVersion` and `Mainline` calculation
strategies. [Mainline](/docs/reference/modes/mainline) interprets commits and
merges to calculate version increments; it is a strategy, not a deployment mode.
You do not need to override `strategies` or set `next-version` to use this preset.

For overrides, follow the [v7 configuration layout](/docs/reference/configuration#v7-configuration-layout):
`workflow` stays at the root, version-calculation settings go under
`calculation`, and output settings go under `output`.

## Branch defaults

These are the defaults of `TrunkBased/preview1`, without overrides:

| Branch type | Recognized names | Increment | Deployment mode | Pre-release label |
| --- | --- | --- | --- | --- |
| main | `main`, `master` | Patch | ContinuousDeployment | Empty |
| feature | `feature/foo`, `features/foo` | Minor | ContinuousDelivery | Branch name, such as `foo` |
| hotfix | `hotfix/fix`, `hotfixes/fix` | Patch | ContinuousDelivery | Branch name, such as `fix` |
| pull-request | `pull/42`, `pull-requests/42`, `pr/42` (including merge refs) | Inherit | ContinuousDelivery | `PullRequest42` |
| unknown | Other names | Patch | ContinuousDelivery | Branch name |

The feature, hotfix, and pull-request patterns also accept `-` as the separator.
Feature, hotfix, and unknown branches list `main` as their source branch type;
pull requests list `main`, `feature`, and `hotfix`. Hotfix branches are marked
`is-release-branch: true`. The preset has no dedicated `develop`, `release`, or
`support` branch configuration: those names use the unknown-branch fallback.

On `main`, ContinuousDeployment produces versions without a pre-release suffix.
An untagged commit can therefore have a stable-looking version; that alone does
not mean it has been published. Feature and hotfix builds carry branch labels.
Commit-message incrementing is enabled, and the default tag prefix accepts
both `1.2.0` and `v1.2.0`.

See the [complete built-in configuration](/docs/reference/configuration#global-configuration)
for the exact regular expressions, merge handling, and output weights. Do not
substitute the outputs of customized GitFlow Mainline examples for this preset.

## Try representative histories

The [worked examples](examples) show direct commits on main, feature and hotfix
merges, release tags, and commit-message increments using this exact preset.
Compare those histories with your repository, including your actual merge
policy, before switching workflows. Squash and fast-forward merges produce
different histories from the explicit merge commits in these examples.
