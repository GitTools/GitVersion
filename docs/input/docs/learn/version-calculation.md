---
Title: Version calculation algorithm
Description: Follow strategy candidates, increments, selection, and deployment modes.
Order: 20
---

GitVersion gathers candidate versions from enabled strategies and selects the
highest **incremented semantic version**. Several strategies can contribute to
one calculation. The order of entries in `strategies` does not define their
priority.

This page expands [how versions are calculated][how-it-works] into decision
diagrams. A [workflow][workflows] supplies defaults, a strategy supplies a
candidate, and a [deployment mode][deployment-modes] processes the selected
candidate. Read each scenario together with its effective configuration.

## On this page

- [Calculation overview][overview-section]
- [All calculation strategies][strategies-section]
- [Branch inheritance][inheritance-section]
- [Candidate increments][increments-section]
- [Select the version and its sources][selection-section]
- [Mainline traversal][mainline-section]
- [All deployment modes][deployment-section]
- [Versioning scenario map][scenarios-section]
- [Executable checkpoints][checkpoints-section]
- [Diagram maintenance][maintenance-section]

## Calculation overview

<pre class="mermaid" aria-label="Version calculation from branch configuration through candidate selection to output">
^"../../../diagrams/VersionCalculation_Overview.mmd"
</pre>

A tag on the current commit can determine the result when it matches the
branch-label rules and the configuration prevents incrementing tagged commits.
When the branch increment requires inheritance, GitVersion resolves candidate
configurations before repeating this tag check with the selected configuration.

Otherwise, GitVersion collects candidates, compares their incremented versions,
resolves source metadata, and applies deployment behavior. The final numeric
version cannot be lower than the eligible branch tag selected by the final tag
check. This check compares major, minor, and patch without the prerelease label.
It can update semantic provenance while retaining the counting anchor.

An empty branch history or a calculation with no accepted base candidates
produces an error. Output formatting happens after version calculation.

## All calculation strategies

<pre class="mermaid" aria-label="Seven version strategies and conditional fallback candidate discovery">
^"../../../diagrams/VersionCalculation_Strategies.mmd"
</pre>

The branches in this diagram represent independent candidate producers. They
do not imply parallel execution or a priority order. Each strategy checks its
enabled flag and eligibility conditions. GitVersion applies candidate ignore
filters before adding a candidate to the comparison.

| Strategy | When it can contribute | Candidate evidence |
| --- | --- | --- |
| `ConfiguredNextVersion` | `next-version` exists, parses, and matches branch-label rules. | The configured version, with its label operation; this is an external semantic source. |
| `TaggedCommit` | The tag service finds eligible semantic versions. | Tags selected according to prefix, format, label, history, and branch tracking; configured and message-derived increments apply. |
| `MergeMessage` | Merge-message tracking is enabled and a parsable message supplies a version from a configured release branch. | The version and message commit; the merged-branch prevent-increment setting controls its increment. |
| `VersionInBranchName` | The effective configuration describes a release branch, and the current or inherited branch name contains a version. | The branch-name version and label, with no explicit numeric increment; this is an external semantic source. |
| `TrackReleaseBranches` | The branch tracks releases, and a related release has a target version and a merge base with the current branch. | A version in the release name, or an eligible prerelease tag on the release's own history; the current branch's increment applies. |
| `Mainline` | Mainline is enabled. | A candidate calculated by traversing commit, tag, branch, and merge history. |
| `Fallback` | It is enabled and no other strategy produced an accepted candidate for this effective configuration. | A starting version with an increment derived from effective configuration and relevant commit messages. |

`TrackReleaseBranches` prefers a versioned release name. Its tag alternative
examines the release's first-parent history while excluding main's first-parent
history. It treats the target as a stable numeric version before calculating
the tracking branch's increment.

Fallback is considered separately for each effective configuration. It is not
an unconditional final version of `0.1.0`. If every candidate is excluded or no
enabled strategy can produce one, calculation fails.

See [calculation strategies][strategies] for source semantics and
[configuration][configuration] for the tracking and ignore controls.

## Branch inheritance

<pre class="mermaid" aria-label="Resolving explicit increments, inherited source branches, pull request targets and orphaned branches">
^"../../../diagrams/VersionCalculation_Inheritance.mmd"
</pre>

An explicit branch increment produces an effective configuration directly.
`Inherit` follows allowed source branches and combines their configuration with
the child branch's settings. A recognized pull-request target can supply this
inheritance source. The traversal guards against cycles.

Several possible sources can produce several effective configurations. Their
strategy candidates enter the same version comparison. An orphaned branch is
skipped when the global increment also requires inheritance; otherwise global
fallback configuration supplies the remaining settings.

## Candidate increments

Each candidate carries a base version, source information, and an increment
operation. Strategies using the shared increment finder follow these rules:

1. Select the relevant history between the candidate source and current commit.
   Apply ignore rules and omit history already covered by eligible version tags.
2. Apply the commit-message mode: disabled, enabled, or merge messages only.
3. Within a message, check no-bump, major, minor, and patch patterns in that
   order. Combine the increments across the selected commits. A matching
   version-bump reset changes accumulation and ends the scan at that point.
4. Use the branch increment when no message override supplies one and an
   increment is requested. The branch increment normally sets the minimum;
   reset behavior can override that minimum.
5. Apply the candidate's label, force-increment, and alternative-version rules.

Some strategies explicitly supply an increment of `None`. Mainline also folds
its own traversal operations. A no-bump message is therefore not a universal
instruction to suppress every strategy's operation. See
[version increments][increments] for the configurable controls.

## Select the version and its sources

<pre class="mermaid" aria-label="Selecting the maximum incremented version, resolving equal-version ties and choosing the counting anchor">
^"../../../diagrams/VersionCalculation_Selection.mmd"
</pre>

GitVersion compares the incremented candidates using semantic-version ordering.
When several candidates with source commits produce the same maximum version,
the latest source timestamp selects the candidate and counting source.

Otherwise, the semantic winner remains the maximum candidate, while the
count-source search prefers a candidate with a source commit, ordered by
incremented version and then source timestamp. For a stable winning version,
this search excludes prerelease baselines. If no source-backed candidate
remains, the highest remaining external candidate supplies a null counting
anchor.

The **semantic source** explains where the version came from. The
**commit-count source** anchors counting through the Git graph. They can differ:
a configured version can supply the semantic baseline while a tag supplies
the counting anchor. A null anchor counts all reachable, non-ignored history.
Counting is not a count of builds or a first-parent distance.

Use the `SemVerSource*` and `CommitCountSource*` [variables][variables] to inspect
these sources. In a candidate trace, keep these fields separate:

| Field | What it explains |
| --- | --- |
| Strategy and effective branch | Which rules produced the candidate. |
| Base version and semantic source | Which tag, message, branch name, configuration value, or traversal supplied the version. |
| Increment and incremented version | Which operation was applied and which value entered the comparison. |
| Counting source | Which commit anchors the history count. |
| Outcome | Selected, lower version, filtered, disabled, ineligible, fallback skipped, or error. |

## Mainline traversal

<pre class="mermaid" aria-label="Mainline commit traversal, increment rules, accumulated operations and unsupported cross-mainline merges">
^"../../../diagrams/VersionCalculation_Mainline.mmd"
</pre>

Mainline gathers tags and branch context, builds recursive iterations for
history, and applies matching commit rules. Those rules produce baseline
operands and increment operators, which are folded into a candidate for the
shared selection process.

The rule families distinguish these events on trunk/main and non-trunk
branches:

| Event | Trunk/main | Non-trunk |
| --- | --- | --- |
| Ordinary commit | Direct-commit rule. | Direct-commit rule. |
| Stable tag | Tagged-commit and last-commit rules. | Tagged-commit and last-commit rules. |
| Prerelease tag | Tagged-commit and last-commit rules. | Tagged-commit and last-commit rules. |
| Merge | Merge and last-merge rules. | Merge and last-merge rules. |
| Branch point | Branching to trunk or non-trunk. | Branching to trunk or non-trunk. |
| First release commit | No dedicated trunk rule. | Dedicated first-release-commit rule. |

These event families do not imply one fixed bump for each event. Tags,
configuration, accumulated context, and commit messages determine which
operations apply.

A recognized merge between two branches configured as main branches currently
has an unsupported path in Mainline. A current-commit tag shortcut can bypass
that traversal. The merged-commit helper also rejects a merge with multiple
source parents. Include these failure paths when investigating a history.

## All deployment modes

<pre class="mermaid" aria-label="Manual deployment, continuous delivery and continuous deployment after candidate selection">
^"../../../diagrams/VersionCalculation_DeploymentModes.mmd"
</pre>

| Mode | Processing after candidate selection |
| --- | --- |
| `ManualDeployment` | Preserves the candidate's prerelease state and attaches build metadata. |
| `ContinuousDelivery` | Requires a numbered prerelease; adds commits since the counting source minus one to that number and records source distance. |
| `ContinuousDeployment` | Removes the prerelease tag and records source distance. |

Tagged-commit shortcuts use their own metadata handling and bypass this normal
dispatch. ContinuousDeployment removes the tag's prerelease component there
as well. In the normal path, the branch-tag numeric floor follows mode
processing.

## Versioning scenario map

These dimensions describe the scenario families across the calculation. They
combine: the branch name alone does not choose a strategy. Arbitrary Git
histories and custom configurations create more combinations than any finite
example catalogue can enumerate.

| Dimension | Scenarios |
| --- | --- |
| Workflow | GitFlow, GitHubFlow, trunk-based, custom defaults, switching workflows, and Mainline strategy combinations. |
| Branch role | Main, develop, feature, release, hotfix, support, pull request, and other branches; feature branches originating from releases. |
| Inheritance | Explicit increment, one or several source branches, PR target, orphaned branch, and a branch without unique commits. |
| Repository context | Local and remote refs, worktrees, tag checkout, detached context, and historical commits. |
| Version evidence | Configured version, branch-name version, merge message, stable tag, prerelease tag, competing evidence, and fallback. |
| Tag tracking | Current commit, ancestor, merge target, main or release branch; prefix, format, label compatibility, and ignored evidence. |
| Release tracking | Versioned name, unversioned release with eligible prerelease tag, several releases, missing merge base, and main-history tag exclusion. |
| Topology | Linear commits, branch points, fast-forward, merge commits, nested or repeated merges, reverse merges, deleted source refs, and unsupported merge shapes. |
| Increment | None, Patch, Minor, Major, Inherit, message modes, no-bump, bump reset, already-tagged history, and prevent-increment controls. |
| Label | Stable, prerelease, branch-derived or SHA labels; matching and mismatching labels and numeric prerelease identifiers. |
| Ignore | Commit SHA, date, path, and reference controls affecting discovery, inheritance, or counting. |
| Selection | One or several strategies/configurations, equal-version ties, distinct semantic/count sources, skipped fallback, and no candidate. |
| Finalization | All three deployment modes, source distance, higher branch-tag floor, provenance, and output formatting. |
| Failure | Empty history, no accepted candidates, invalid version/configuration, delivery without numbered prerelease, and unsupported merges. |

The [GitFlow examples][gitflow-examples], [GitHubFlow examples][githubflow-examples],
and [trunk-based examples][trunkbased-examples] show histories with asserted
versions. The [integration scenarios][integration-scenarios] and
[calculation tests][calculation-tests] provide the larger executable catalogue.

## Executable checkpoints

These histories are generated by tests that assert the displayed versions.
The calculation examples also assert the semantic source, counting source,
and distance. They provide representative checkpoints for the rules above;
the algorithm flowcharts themselves remain authored diagrams.

### Competing candidates

With GitHubFlow defaults, a configured `next-version` of `5.0.0` wins over
the incremented `1.0.0` tag. It supplies an external semantic source, while
the tag remains the counting source. Lowering `next-version` to `0.5.0`
makes the tag candidate win instead.

<pre class="mermaid" aria-label="Test-generated history where configured version wins over the tag candidate">
^"../../../diagrams/DocumentationSamplesForVersionCalculation_CompetingConfiguredAndTaggedVersions_ConfigurationWins.mmd"
</pre>

<pre class="mermaid" aria-label="Test-generated history where the tag candidate wins over configured version">
^"../../../diagrams/DocumentationSamplesForVersionCalculation_CompetingConfiguredAndTaggedVersions_TagWins.mmd"
</pre>

### Inherited increments

These GitHubFlow examples set the feature branch increment to `Inherit`.
Changing its source branch `main` from `Patch` to `Minor` changes the
feature version from `1.0.1-work.1+1` to `1.1.0-work.1+1`.

<pre class="mermaid" aria-label="Test-generated feature history inheriting a patch increment">
^"../../../diagrams/DocumentationSamplesForVersionCalculation_FeatureInheritsSourceIncrement_Patch.mmd"
</pre>

<pre class="mermaid" aria-label="Test-generated feature history inheriting a minor increment">
^"../../../diagrams/DocumentationSamplesForVersionCalculation_FeatureInheritsSourceIncrement_Minor.mmd"
</pre>

### Equal versions and separate sources

The tie example supplies two controlled strategy candidates of `2.0.0`,
anchored at consecutive commits in a real repository. It isolates selection
from strategy discovery: the latest commit supplies both sources, leaving
one commit to count. ManualDeployment produces `2.0.0+1`.

<pre class="mermaid" aria-label="Test-generated selection history with two controlled equal-version candidates">
^"../../../diagrams/DocumentationSamplesForVersionCalculation_EqualCandidatesUseLatestSource.mmd"
</pre>

In the GitFlow release merge below, the merge message supplies `2.0.0`.
The older `1.0.0` tag supplies the counting source. The release commit,
merge commit, and following main commit all count, giving a distance of three.

<pre class="mermaid" aria-label="Test-generated release merge with distinct semantic and counting sources">
^"../../../diagrams/DocumentationSamplesForVersionCalculation_MergeMessageAndCountingSourceDiffer.mmd"
</pre>

### Deployment modes on the same history

Each example uses GitHubFlow with the main branch label set to `beta` and
the named deployment mode. Two commits after `1.0.0` give the same semantic
source, patch increment, and counting distance in all three modes.

| Mode | Asserted full version |
| --- | --- |
| `ManualDeployment` | `1.0.1-beta.1+2` |
| `ContinuousDelivery` | `1.0.1-beta.2` |
| `ContinuousDeployment` | `1.0.1` |

<pre class="mermaid" aria-label="Test-generated manual deployment history">
^"../../../diagrams/DocumentationSamplesForVersionCalculation_DeploymentModesProcessSameHistory_ManualDeployment.mmd"
</pre>

<pre class="mermaid" aria-label="Test-generated continuous delivery history">
^"../../../diagrams/DocumentationSamplesForVersionCalculation_DeploymentModesProcessSameHistory_ContinuousDelivery.mmd"
</pre>

<pre class="mermaid" aria-label="Test-generated continuous deployment history">
^"../../../diagrams/DocumentationSamplesForVersionCalculation_DeploymentModesProcessSameHistory_ContinuousDeployment.mmd"
</pre>

### Tags and commit messages

These trunk-based examples check tag shortcuts and commit-message increments.

<pre class="mermaid" aria-label="Test-generated history of direct commits and release tags">
^"../../../diagrams/DocumentationSamplesForTrunkBased_DirectCommitsAndReleaseTags.mmd"
</pre>

<pre class="mermaid" aria-label="Test-generated history of patch, minor and major commit-message increments">
^"../../../diagrams/DocumentationSamplesForTrunkBased_CommitMessageIncrements.mmd"
</pre>

## Diagram maintenance

These algorithm flowcharts are authored Mermaid sources in `docs/diagrams`,
included in the page with the same renderer and shared theme as the workflow
examples. The existing syntax validator checks all `.mmd` files:

```bash
node docs/scripts/validate-mermaid.mjs
```

The `GenerateMermaidSources` task regenerates test-derived
`DocumentationSamplesFor*.mmd` scenario diagrams; it leaves these algorithm
flowcharts intact. See [contributing examples][contributing-examples] when
changing a worked history and its asserted versions.

The implementation entry points for maintaining this map are
[candidate calculation][candidate-calculation],
[configuration inheritance][configuration-inheritance],
[increment selection][increment-selection], and
[Mainline traversal][mainline-traversal]. Update the diagram and accompanying
explanation when those decision rules change.

[how-it-works]: /docs/learn/how-it-works

[overview-section]: #calculation-overview

[strategies-section]: #all-calculation-strategies

[inheritance-section]: #branch-inheritance

[increments-section]: #candidate-increments

[selection-section]: #select-the-version-and-its-sources

[mainline-section]: #mainline-traversal

[deployment-section]: #all-deployment-modes

[scenarios-section]: #versioning-scenario-map

[checkpoints-section]: #executable-checkpoints

[maintenance-section]: #diagram-maintenance

[workflows]: /docs/learn/workflows-modes-strategies

[deployment-modes]: /docs/reference/modes

[strategies]: /docs/reference/version-sources

[configuration]: /docs/reference/configuration

[increments]: /docs/reference/version-increments

[variables]: /docs/reference/variables

[gitflow-examples]: /docs/learn/branching-strategies/gitflow/examples

[githubflow-examples]: /docs/learn/branching-strategies/githubflow/examples

[trunkbased-examples]: /docs/learn/branching-strategies/trunkbased/examples

[integration-scenarios]: https://github.com/GitTools/GitVersion/tree/main/src/GitVersion.Core.Tests/IntegrationTests

[calculation-tests]: https://github.com/GitTools/GitVersion/tree/main/src/GitVersion.Core.Tests/VersionCalculation

[contributing-examples]: /docs/learn/branching-strategies/contribute-examples

[candidate-calculation]: https://github.com/GitTools/GitVersion/blob/main/src/GitVersion.Core/VersionCalculation/VersionCalculators/NextVersionCalculator.cs

[configuration-inheritance]: https://github.com/GitTools/GitVersion/blob/main/src/GitVersion.Core/VersionCalculation/EffectiveBranchConfigurationFinder.cs

[increment-selection]: https://github.com/GitTools/GitVersion/blob/main/src/GitVersion.Core/VersionCalculation/IncrementStrategyFinder.cs

[mainline-traversal]: https://github.com/GitTools/GitVersion/blob/main/src/GitVersion.Core/VersionCalculation/VersionSearchStrategies/MainlineVersionStrategy.cs
