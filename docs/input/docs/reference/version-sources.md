---
Order: 50
Title: Calculation strategies
Description: Sources and algorithms used to determine a version.
RedirectFrom: docs/more-info/version-sources
---
Strategies are selected with the [`strategies` configuration setting](/docs/reference/configuration#strategies). They work with branch configuration and deployment modes; they do not independently define the final version.

| Strategy | Role |
| --- | --- |
| `ConfiguredNextVersion` | Uses `next-version` as a candidate. |
| `TaggedCommit` | Finds applicable semantic versions in tags. |
| `MergeMessage` | Extracts version information from matching merge messages. |
| `VersionInBranchName` | Extracts a version from a matching branch name. |
| `TrackReleaseBranches` | Considers release-branch history according to effective branch configuration. |
| `Mainline` | Calculates increments while walking history using mainline rules. |
| `Fallback` | Supplies a starting candidate when no other selected strategy provides one; its increment is determined from configuration. |

<a id="version-sources"></a>

## Selection and increments

The order of entries in `strategies` is not a priority list. GitVersion evaluates candidates and selects a next version, preserving its semantic source separately from the commit-count anchor. Fallback is deferred until other strategies have been considered.

Do not interpret a source as “always increments” or “never increments” without the relevant branch configuration. Tagged commits, merge handling, increment inheritance, and deployment mode affect the outcome.

For the sequence of operations, see [how versions are calculated](/docs/learn/how-it-works).

## Semantic source and commit-count source

The **semantic source** supplies the baseline version and its final increment. Tags and merge messages have associated commits. Configuration values and branch names are external sources: they do not intrinsically belong to a commit. Mainline can fold several operations into a derived baseline while retaining its source provenance. The source is not a detector for every commit that contains a version-bump message.

The **commit-count source** anchors the count of commits reachable from the current commit, excluding commits reachable from the anchor and applying the configured ignore filters. This is a graph count, not first-parent distance or a count of builds. A null anchor counts all reachable, non-ignored history.

GitVersion compares candidates by their incremented semantic version. For multiple equal candidates with source commits, it selects the latest source timestamp. Otherwise the counting search prefers a commit-backed candidate by descending incremented version and then source timestamp. When the winning version is stable, pre-release base candidates are excluded from this counting search. Only when no commit-backed candidate remains does it use a null anchor. The order in `strategies` does not establish precedence.

For example, a `1.0.0` tag can remain the counting anchor while configuration or the branch name `release/5.0.0` supplies a `5.0.0` baseline. Replacing that anchor with the external semantic source would reset the meaning of the count. A later tag that raises the numeric version is reflected in semantic provenance without changing the selected counting anchor.

See the `SemVerSource*` and `CommitCountSource*` [output variables](/docs/reference/variables). The legacy `VersionSource*` fields retain their existing values for compatibility; they mix the two concepts.

## Existing source topics

The following anchors are retained for older links.

### Tag name

See `TaggedCommit` above and [tag-prefix](/docs/reference/configuration#tag-prefix).

### Version in branch name

See `VersionInBranchName` and [version-in-branch-pattern](/docs/reference/configuration#version-in-branch-pattern).

### Merge message

See `MergeMessage` and [merge-message-formats](/docs/reference/configuration#merge-message-formats).

### GitVersion.yml

See `ConfiguredNextVersion` and [next-version](/docs/reference/configuration#next-version).

### Develop branch

See `TrackReleaseBranches` and the [GitFlow configuration](/docs/reference/configuration#global-configuration).

### Fallback

Fallback determines an increment using the effective configuration. Do not rely on a fixed final version independent of branch settings.

<a id="others"></a>

For configuration combinations, see [workflow examples](/docs/learn/branching-strategies). To propose additional strategy behavior, start a [discussion](https://github.com/GitTools/GitVersion/discussions).
