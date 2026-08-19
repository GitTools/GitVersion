---
Order: 50
Title: GitHubFlow Examples
RedirectFrom: docs/git-branching-strategies/githubflow-examples
---

These examples are illustrating the usage of the supported `GitHubFlow` workflow
in GitVersion. To enable this workflow, the builtin template
[GitHubFlow/v1](/docs/workflows/GitHubFlow/v1.json) needs to be referenced in the
configuration as follows:

```yaml
calculation:
  mode: ContinuousDelivery
  workflow: GitHubFlow/v1
```

Where
the [continuous deployment][continuous-deployment] mode for no branches,
the [continuous delivery][continuous-delivery] mode for
`main` branch and
the [manual deployment][manual-deployment] mode
for `release`, `feature` and `unknown` branches are specified.

This configuration allows you to publish CI (Continuous Integration) builds
from `main` branch to an artifact repository.
All other branches are manually published. Read more about this at
[version increments](/docs/reference/version-increments).

:::{.alert .alert-info}
The _continuous delivery_ mode has been used for the `main` branch in this
examples (specified as a fallback on the root
configuration layer) to illustrate how the version increments are applied.
In production context the _continuous deployment_ mode might be a better
option when e.g. the release process is automated or the commits are tagged
by the pipeline automatically.
:::

## Feature Branch

Feature branches can be used in the `GitHubFlow` workflow to implement a
feature or fix a bug in an isolated environment. Feature branches will take
the feature
branch name and use that as the pre-release label. Feature branches will be
created from a `main` or `release` branch.

### Create feature branch from main

<pre class="mermaid" aria-label="GitHubFlow feature branch diagram">
^"../../../../../diagrams/DocumentationSamplesForGitHubFlow_FeatureBranch.mmd"
</pre>

:::{.alert .alert-info}
After the feature branch is merged, the version on `main` is `2.0.0-5`.
This is due to `main` running in _continuous delivery_ mode. If `main` was
configured to use _continuous deployment_ the version would be `2.0.0`.
:::

### Mainline version strategy comparison

The same feature branch history produces different versions when the
[Mainline version strategy](/docs/reference/modes/mainline) is
used. In particular, the final untagged commit on `main` becomes `2.0.1-1`
rather than `2.0.0-6`:

<pre class="mermaid" aria-label="GitHubFlow feature branch using the Mainline version strategy diagram">
^"../../../../../diagrams/DocumentationSamplesForGitHubFlow_FeatureBranchWithMainline.mmd"
</pre>

The other Mainline integration scenarios use the same branch histories as the
release examples below, so they are not repeated here. See
[`DocumentationSamplesForGitHubFlow.cs`](https://github.com/GitTools/GitVersion/blob/main/src/GitVersion.Core.Tests/IntegrationTests/DocumentationSamplesForGitHubFlow.cs)
for the complete executable scenarios and their expected versions.

## Release Branches

Release branches are used for major, minor and patch releases to stabilize a RC
(Release Candidate) or to integrate features/hotfixes (in parallel) targeting
different
iterations. Release branches are taken from `main` and will
be merged back afterwards. Finally the `main` branch is tagged with the
released version.

Release branches can be used in the `GitHubFlow` as well as `GitFlow` workflow.
Sometimes you
want to start on a large feature which may take a while to stabilize so you want
to keep it off main. In these scenarios you can either create a long lived
feature branch (if you do not know the version number this large feature will go
into, and it's non-breaking) otherwise you can create a release branch for the
next major version. You can then submit pull requests to the long lived feature
branch or the release branch.

### Create release branch

<pre class="mermaid" aria-label="GitHubFlow release branch diagram">
^"../../../../../diagrams/DocumentationSamplesForGitHubFlow_ReleaseBranch.mmd"
</pre>

### Create release branch with version

<pre class="mermaid" aria-label="GitHubFlow versioned release branch diagram">
^"../../../../../diagrams/DocumentationSamplesForGitHubFlow_VersionedReleaseBranch.mmd"
</pre>

## To Contribute

See [contributing examples](/docs/learn/branching-strategies/contribute-examples).

### Source

The diagrams are generated from `DocumentationSamplesForGitHubFlow.cs`. After
changing a scenario, run the `GenerateMermaidSources` Cake task, inspect the
updated Mermaid source, and run the `ValidateMermaidDiagrams` Cake task before
committing it.

[continuous-deployment]: /docs/reference/modes/continuous-deployment

[continuous-delivery]: /docs/reference/modes/continuous-delivery

[manual-deployment]: /docs/reference/modes/manual-deployment
