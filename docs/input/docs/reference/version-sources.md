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

The order of entries in `strategies` is not a priority list. GitVersion evaluates candidates and selects a next version with a corresponding version source. Fallback is deferred until other strategies have been considered.

Do not interpret a source as “always increments” or “never increments” without the relevant branch configuration. Tagged commits, merge handling, increment inheritance, and deployment mode affect the outcome.

For the sequence of operations, see [how versions are calculated](/docs/learn/how-it-works).

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
