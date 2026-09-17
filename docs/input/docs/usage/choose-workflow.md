---
Title: Choose a workflow
Order: 1
---
Choose defaults that match how you merge and release. A workflow is a configuration preset, not an instruction to change your branching process.

| Workflow | Starting point | Configuration value |
| --- | --- | --- |
| GitHub Flow | A main branch with short-lived branches merged through pull requests. | `GitHubFlow/v1` |
| Git Flow | Development and release branches with distinct release stages. | `GitFlow/v1` |
| Trunk-based (preview) | Evaluate the experimental trunk-based configuration against your own histories. | `TrunkBased/preview1` |

For example, create `GitVersion.yml` at the repository root:

```yaml
workflow: GitHubFlow/v1
```

Inspect the resolved settings with `dotnet-gitversion --show-config`. Compare the results on representative feature, tagged, and release commits before adopting changes.

The preview workflow is experimental. Do not assume it has the same compatibility guarantees as the versioned GitFlow and GitHubFlow presets.

## Understand the choices

A workflow supplies defaults. A [deployment mode][deployment-mode] controls version behavior between releases. A [calculation strategy][calculation-strategy] helps determine the version from history. **Mainline is a calculation strategy**, not a fourth deployment mode.

Read [workflows, modes, and strategies][workflows-modes-and-strategies] before combining custom settings.

## Worked examples and defaults

- [GitHub Flow][github-flow] and [examples][examples].
- [Git Flow][git-flow] and [examples][examples-2].
- [Trunk-based (preview)][trunk-based-preview] and [examples][examples-3].
- [Built-in configurations][built-in-configurations].
- [Create and inspect configuration][create-and-inspect-configuration].

[deployment-mode]: /docs/reference/modes

[calculation-strategy]: /docs/reference/version-sources

[workflows-modes-and-strategies]: /docs/learn/workflows-modes-strategies

[github-flow]: /docs/learn/branching-strategies/githubflow

[examples]: /docs/learn/branching-strategies/githubflow/examples

[git-flow]: /docs/learn/branching-strategies/gitflow

[examples-2]: /docs/learn/branching-strategies/gitflow/examples

[trunk-based-preview]: /docs/learn/branching-strategies/trunkbased

[examples-3]: /docs/learn/branching-strategies/trunkbased/examples

[built-in-configurations]: /docs/reference/configuration#global-configuration

[create-and-inspect-configuration]: /docs/usage/configure
