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

A workflow supplies defaults. A [deployment mode](/docs/reference/modes) controls version behavior between releases. A [calculation strategy](/docs/reference/version-sources) helps determine the version from history. **Mainline is a calculation strategy**, not a fourth deployment mode.

Read [workflows, modes, and strategies](/docs/learn/workflows-modes-strategies) before combining custom settings.

## Worked examples and defaults

- [GitHub Flow](/docs/learn/branching-strategies/githubflow) and [examples](/docs/learn/branching-strategies/githubflow/examples).
- [Git Flow](/docs/learn/branching-strategies/gitflow) and [examples](/docs/learn/branching-strategies/gitflow/examples).
- [Built-in configurations](/docs/reference/configuration#global-configuration).
- [Create and inspect configuration](/docs/usage/configure).
