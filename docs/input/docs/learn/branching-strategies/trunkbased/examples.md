---
Order: 50
Title: Trunk-based examples
---

All examples use the unmodified [TrunkBased preview preset](/docs/learn/branching-strategies/trunkbased):

```yaml
workflow: TrunkBased/preview1
```

Each scenario starts independently on `main` with a commit tagged `1.2.0`.
The diagrams display asserted **FullSemVer** values.

## Direct commits and release tags

Two ordinary commits on main produce `1.2.1` and `1.2.2`. Tagging the latter
`v1.2.2` preserves that version; the following commit produces `1.2.3`.
GitVersion calculates these values; it does not create the release tags for you.
Tag the commit you release, using the version you published.

<pre class="mermaid" aria-label="Trunk-based direct commits and release tags diagram">
^"../../../../../diagrams/DocumentationSamplesForTrunkBased_DirectCommitsAndReleaseTags.mmd"
</pre>

## Feature merge

Create `feature/foo` from the tagged main commit, make two commits, and merge
it into main with `git merge --no-ff feature/foo`. The feature defaults to a
Minor increment: the branch builds are `1.3.0-foo.1` and `1.3.0-foo.2`.
The merge produces `1.3.0` on main, and the next ordinary main commit produces
`1.3.1`. Delete the feature branch after merging.

<pre class="mermaid" aria-label="Trunk-based feature merge diagram">
^"../../../../../diagrams/DocumentationSamplesForTrunkBased_feature_BranchMerge.mmd"
</pre>

## Hotfix merge

Create `hotfix/fix` from the tagged main commit, make two commits, and merge
it with `git merge --no-ff hotfix/fix`. The hotfix defaults to a Patch increment:
branch builds are `1.2.1-fix.1` and `1.2.1-fix.2`. The merge produces
`1.2.1` on main, and the next ordinary main commit produces `1.2.2`.
Delete the hotfix branch after merging.

<pre class="mermaid" aria-label="Trunk-based hotfix merge diagram">
^"../../../../../diagrams/DocumentationSamplesForTrunkBased_hotfix_BranchMerge.mmd"
</pre>

## Commit-message increments

On main, the following sequence illustrates the enabled commit-message rules:

| Commit message | FullSemVer |
| --- | --- |
| `Fix a bug +semver: patch` | `1.2.1` |
| `Add an API +semver: minor` | `1.3.0` |
| `Break an API +semver: major` | `2.0.0` |
| Ordinary commit | `2.0.1` |

The default patterns also accept `fix`, `feature`, and `breaking` as aliases
for `patch`, `minor`, and `major`. See
[version increments](/docs/reference/version-increments) for the configurable
message patterns.

<pre class="mermaid" aria-label="Trunk-based commit-message increments diagram">
^"../../../../../diagrams/DocumentationSamplesForTrunkBased_CommitMessageIncrements.mmd"
</pre>

## Executable source

These diagrams come from
[`DocumentationSamplesForTrunkBased.cs`](https://github.com/GitTools/GitVersion/blob/main/src/GitVersion.Core.Tests/IntegrationTests/DocumentationSamplesForTrunkBased.cs),
which constructs `TrunkBasedConfigurationBuilder.New.Build()` in each scenario
and asserts every displayed version. To update them, run the
`GenerateMermaidSources` Cake task and then `ValidateMermaidDiagrams`.
See [contributing examples](/docs/learn/branching-strategies/contribute-examples).
