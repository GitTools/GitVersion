---
Title: Workflows, modes, and strategies
Order: 15
---
GitVersion has three separate configuration choices. They work together, but they are not interchangeable.

| Choice | Question it answers | Examples |
| --- | --- | --- |
| Workflow | Which configuration defaults should I start with? | `GitHubFlow/v1`, `GitFlow/v1`, `TrunkBased/preview1` (experimental). |
| Deployment mode | How should versions behave between releases? | `ManualDeployment`, `ContinuousDelivery`, `ContinuousDeployment`. |
| Calculation strategies | Which sources and algorithms should determine the version? | `TaggedCommit`, `MergeMessage`, `Mainline`. |

## Workflows provide defaults

A workflow loads a built-in configuration. Your configuration can override it. [Choose a workflow](/docs/usage/choose-workflow) and inspect the result with `dotnet-gitversion --show-config`.

## Deployment modes shape the result

The effective branch configuration determines the deployment mode. Compare the [deployment modes](/docs/reference/modes) and their examples before selecting one. The same commit count does not imply the same pre-release version under every mode.

## Strategies examine history

Strategies find candidate versions or calculate increments from history. They are configured with `strategies`. Mainline belongs to this set; it is not a workflow name or an additional deployment mode.

See the [strategy reference](/docs/reference/version-sources) for each strategy's role, then read [how calculation works](/docs/learn/how-it-works).

## Keep output separate

After calculation, GitVersion exposes [variables](/docs/reference/variables). CLI output options choose how to consume them; assembly and custom formatting settings produce additional version representations. Changing the output destination does not select a new workflow.
