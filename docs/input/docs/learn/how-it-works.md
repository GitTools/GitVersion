---
Order: 10
Title: How versions are calculated
RedirectFrom: docs/more-info/how-it-works
---
GitVersion combines repository history with effective configuration to calculate a version for the current commit. It then exposes that result as [version variables][version-variables].

<a id="architecture"></a>

## 1. Resolve the context and configuration

GitVersion determines the current commit and branch and resolves configuration defaults and overrides. The effective branch configuration supplies matching rules, increment behavior, labels, and deployment mode.

Missing tags, branches, or history can change the information available to the calculation. Start with the [repository requirements][repository-requirements].

<a id="version-strategies"></a>

## 2. Consider tags and version strategies

A suitable version tag on the current commit can determine the result when the effective configuration prevents incrementing an already-tagged commit. This is conditional: a tagged commit does not unconditionally bypass all other calculation.

Otherwise, enabled [version strategies][version-strategies] examine sources such as tags, merge messages, release branches, and configured versions. Candidates carry information about the base version and its source in history.

Fallback is considered after other strategies and is skipped when another strategy has returned a base version. It is not an unconditional fixed final version of 0.1.0.

## 3. Apply increments and deployment behavior

GitVersion compares candidate next versions, resolves the version source, and applies effective branch and [deployment-mode][deployment-mode] rules. Branch settings, commit messages, tags, and merge history can all affect the result.

Read [version increments][version-increments] for increment controls, and [workflows, modes, and strategies][workflows-modes-and-strategies] for their different responsibilities.

## 4. Produce version variables

The result is expanded into semantic versions, assembly versions, branch and commit information, and other variables. Formatting settings control additional representations such as assembly and informational versions.

Choose [JSON, a single variable, a file, or build-server output][json-a-single-variable-a-file-or-build-server-output] according to how your build consumes the result.

## Follow an example

The [GitHubFlow examples][githubflow-examples] and [GitFlow examples][gitflow-examples] show concrete histories and their expected versions. Always read an example together with its configuration.

If your output is unexpected, follow [Troubleshooting][troubleshooting] before changing version settings.

[version-variables]: /docs/reference/variables

[repository-requirements]: /docs/reference/requirements

[version-strategies]: /docs/reference/version-sources

[deployment-mode]: /docs/reference/modes

[version-increments]: /docs/reference/version-increments

[workflows-modes-and-strategies]: /docs/learn/workflows-modes-strategies

[json-a-single-variable-a-file-or-build-server-output]: /docs/usage/cli/output

[githubflow-examples]: /docs/learn/branching-strategies/githubflow/examples

[gitflow-examples]: /docs/learn/branching-strategies/gitflow/examples

[troubleshooting]: /docs/learn/faq
