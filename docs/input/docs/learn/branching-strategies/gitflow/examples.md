---
Order: 40
Title: GitFlow Examples
RedirectFrom:
- docs/git-branching-strategies/gitflow-examples_complete
- docs/git-branching-strategies/gitflow-examples
---

These examples are illustrating the usage of the supported `GitFlow` workflow
in GitVersion. To enable this workflow, the builtin template
[GitFlow/v1](/docs/workflows/GitFlow/v1.json) needs to be referenced in the
configuration as follows:

```yaml
workflow: GitFlow/v1
mode: ContinuousDelivery
```

Where
the [continuous deployment][continuous-deployment] mode for no branches,
the [continuous delivery][continuous-delivery] mode for
`main`, `support` and `develop` branches and
the [manual deployment][manual-deployment] mode
for `release`, `feature`, `hotfix` and `unknown` branches are specified.

This configuration allows you to publish CI (Continuous Integration) builds
from `main`, `support` and `develop` branches to an artifact repository.
All other branches are manually published. Read more about this at
[version increments](/docs/reference/version-increments).

:::{.alert .alert-info}
The _continuous delivery_ mode has been used for the `main` and the
`support` branch in this examples (specified as a fallback on the root
configuration layer) to illustrate how the version increments are applied.
In production context the _continuous deployment_ mode might be a better
option when e.g. the release process is automated or the commits are tagged
by the pipeline automatically.
:::

## Feature Branches

Feature branches can be used in the `GitFlow` workflow to implement a
feature in an isolated environment. Feature branches will take the feature
branch name and use that as the pre-release label. Feature branches will be
created from a `develop`, `release`, `main`, `support` or `hotfix` branch.

### Create feature branch from main

<pre class="mermaid" aria-label="GitFlow feature branch from main diagram">
^"../../../../../diagrams/DocumentationSamplesForGitFlow_FeatureFromMainBranch.mmd"
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

<pre class="mermaid" aria-label="GitFlow feature branch from main using the Mainline version strategy diagram">
^"../../../../../diagrams/DocumentationSamplesForGitFlow_FeatureFromMainBranchWithMainline.mmd"
</pre>

The other Mainline integration scenarios use the same branch histories as the
examples below, so they are not repeated here. See
[`DocumentationSamplesForGitFlow.cs`](https://github.com/GitTools/GitVersion/blob/main/src/GitVersion.Core.Tests/IntegrationTests/DocumentationSamplesForGitFlow.cs)
for the complete executable scenarios and their expected versions.

### Create feature branch from develop

<pre class="mermaid" aria-label="GitFlow feature branch from develop diagram">
^"../../../../../diagrams/DocumentationSamplesForGitFlow_FeatureFromDevelopBranch.mmd"
</pre>

:::{.alert .alert-info}
After the feature branch is merged, the version on `develop` is
`1.3.0-alpha.3`. This is due to `develop` running in _continuous delivery_
mode. If `develop` was configured to use _manual deployment_ the version
would still be `1.3.0-alpha.1` and you would have to use pre-release tags
to increment the pre-release label `alpha.1`.
:::

## Hotfix Branches

Hotfix branches are used when you need to do a _patch_ release in the
`GitFlow` workflow and are always created from `main` branch.

### Create hotfix branch

<pre class="mermaid" aria-label="GitFlow hotfix branch diagram">
^"../../../../../diagrams/DocumentationSamplesForGitFlow_HotfixBranch.mmd"
</pre>

### Create hotfix branch with version number

<pre class="mermaid" aria-label="GitFlow versioned hotfix branch diagram">
^"../../../../../diagrams/DocumentationSamplesForGitFlow_VersionedHotfixBranch.mmd"
</pre>

## Release Branches

Release branches are used for major and minor releases to stabilize a RC
(Release Candidate) or to integrate features (in parallel) targeting different
iterations. Release branches are taken from `main` (or from `develop`) and will
be merged back afterwards. Finally the `main` branch is tagged with the
released version.

Release branches can be used in the `GitFlow` as well as `GitHubFlow` workflow.
Sometimes you want to start on a large feature which may take a while
to stabilize so you want to keep it off main.
In these scenarios you can either create a long lived
feature branch (if you do not know the version number this large feature will go
into, and it's non-breaking) otherwise you can create a release branch for the
next major version. You can then submit pull requests to the long lived feature
branch or the release branch.

### Create release branch

<pre class="mermaid" aria-label="GitFlow release branch diagram">
^"../../../../../diagrams/DocumentationSamplesForGitFlow_ReleaseBranch.mmd"
</pre>

### Create release branch with version

<pre class="mermaid" aria-label="GitFlow versioned release branch diagram">
^"../../../../../diagrams/DocumentationSamplesForGitFlow_VersionedReleaseBranch.mmd"
</pre>

## Develop Branch

<pre class="mermaid" aria-label="GitFlow develop branch diagram">
^"../../../../../diagrams/DocumentationSamplesForGitFlow_DevelopBranch.mmd"
</pre>

## Support Branches

Support branches are not really covered in GitFlow, but are essential if you
need to maintain multiple major versions at the same time. You could use support
branches for supporting minor releases as well. If you are just supporting the
majors, then name your branch `support/<major>.x` (i.e `support/1.x`), to
support minors use `support/<major>.<minor>.x` or `support/<major>.<minor>.0`.
(i.e `support/1.3.x` or `support/1.3.0`)

<pre class="mermaid" aria-label="GitFlow support branch diagram">
^"../../../../../diagrams/DocumentationSamplesForGitFlow_SupportBranch.mmd"
</pre>

:::{.alert .alert-info}
Depending on what you name your support branch, you may or may not need a
hotfix branch. Naming it `support/1.x` will automatically bump the patch,
if you name it `support/1.3.0` then the version in branch name rule will
kick in and the patch _will not_ automatically bump, meaning you have to
use hotfix branches.
:::

## To Contribute

See [contributing examples](/docs/learn/branching-strategies/contribute-examples).

### Source

The diagrams are generated from `DocumentationSamplesForGitFlow.cs`. After
changing a scenario, run the `GenerateMermaidSources` Cake task, inspect the
updated Mermaid source, and run the `ValidateMermaidDiagrams` Cake task before
committing it.

[continuous-deployment]: /docs/reference/modes/continuous-deployment

[continuous-delivery]: /docs/reference/modes/continuous-delivery

[manual-deployment]: /docs/reference/modes/manual-deployment
