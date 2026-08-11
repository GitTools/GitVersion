---
Order: 10
Title: How it works
RedirectFrom: docs/more-info/how-it-works
---

GitVersion v3 works very differently to v2. Version 2 had knowledge of both
GitFlow and GitHubFlow hard coded into it, with each branch having its own
class which calculated the version for that branch type.

v3 is driven by [configuration](/docs/reference/configuration), meaning most of the
behaviors in GitVersion can be tweaked to work the way you want. This also makes
it _much_ more predictable and easier to diagnose when odd things are happening.

## Architecture

GitVersion has three distinct steps for calculating versions in v3.

1. If the current commit is tagged, the tag is used and build metadata
   (excluding commit count) is added. The other two steps will not execute.
2. A set of strategies are evaluated to decide on the base version and some
   metadata about that version. See [Version Strategies](#version-strategies)
3. The highest base version is selected, using that base version as the new
   version is calculated.

Visually it looks something like this:

<pre class="mermaid" aria-label="Version calculation diagram">
^"../../../diagrams/version-calculation.mmd"
</pre>

[View diagram source](https://github.com/GitTools/GitVersion/blob/main/docs/diagrams/version-calculation.mmd)

**\*** Some strategies allow the version to be incremented, others don't. More
info below.
**+** This version is out of context with the rest of the example. It is here
simply to show what happens if the check is true.

### Version Strategies

Currently we have the following strategies:

* `Fallback` - Always returns 0.0.0 and will be used for
  calculating the next version which is dependent on the increment strategy of
  the effected branch (e.g. on main the next version is 0.0.1 or on develop it is 0.1.0).
  The fallback strategy only applies if no other selected strategy returns a base version.
* `ConfiguredNextVersion` - Returns the version from the GitVersion.yaml file
* `MergeMessage` - Finds version numbers from merge messages
  (e.g., `Merge 'release/3.0.0' into 'main'` will return `3.0.0`)
* `TaggedCommit` - Extracts version information from all tags on the branch which are valid,
  and not newer than the current commit.
* `TrackReleaseBranches` - Considers the base version extracted from release branches when
  calculating the next version for branches configured with `track-release-branches: true`
  (part of default configuration for `develop` branch in `GitFlow` workflow)
* `VersionInBranchName` - Extracts version information from the
  branch name (e.g., `release/3.0.0` will find `3.0.0`)
* `Mainline` - Increments the version on every commit for branches configured with `is-main-branch: true`

Each strategy needs to return an instance of `BaseVersion` which has the
following properties:

* `Source` - Description of the source (e.g., `Merge message 'Merge 'release/3.0.0' into 'main'`)
* `ShouldIncrement` - Some strategies should have the version incremented,
  others do not (e.g., `ConfiguredNextVersion` returns false,
  `TaggedCommit` returns true)
* `SemanticVersion` - SemVer of the base version strategy
* `BaseVersionSource` - SHA hash of the source. Commits will be counted from
  this hash. Can be null (e.g., `ConfiguredNextVersion` returns
  null).
* `BranchNameOverride` - When `useBranchName` or `{BranchName}` is used in the
  tag configuration, this allows the branch name to be changed by a base version.
  `VersionInBranchName` uses this to strip out anything before the
  first `-` or `/.` so `foo` ends up being evaluated as `foo`. If in doubt, just
  use null.
