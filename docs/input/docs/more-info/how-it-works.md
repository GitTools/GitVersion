---
Order: 10
Title: How it works
---

GitVersion v3 works very differently to v2. Version 2 had knowledge of both
GitFlow and GitHubFlow hard coded into it, with each branch having it's own
class which calculated the version for that branch type.

v3 is driven by [configuration](../configuration), meaning most of the
behaviors in GitVersion can be tweaked to work the way you want. This also makes
it *much* more predictable and easier to diagnose when odd things are happening.

## Architecture
GitVersion has three distinct steps for calculating versions in v3.

1. If the current commit is tagged, the tag is used and build metadata
(excluding commit count) is added. The other two steps will not execute.
2. A set of strategies are evaluated to decide on the base version and some
metadata about that version.  These strategies include HighestReachableTag,
NextVersionInConfig, MergedBranchWithVersion, VersionInBranchName etc.
3. The highest base version is selected, using that base version as the new
version is calculated.

Visually it looks something like this:

![Version Calculation](http://www.plantuml.com:80/plantuml/png/fLCxJyCm4DxzAsuib4P914i69De1CS38Vd6kYIN7ZcodK8aVp-KX6Y2fKCbY9NV-7lVb2WoOeoVOMRDNfH0lz1vUoNbbpGwrR3K6ws1p3rlk-bN8u972f2AC3GHEbLN8m1D1Jjg-mPuXAZvx9kL1ZW1KY5dOZczMI0Pf54VnHtf7jpaAWJg0sW-uXw4PK3Eb1sMaevfCW6i1_0m6po1l7HfPJUxvu5XYUOHLWq5MLptCudmMK9--u5glJ0dIEaVo1Dw3JgVM6Km4cM9mzyrQXHuQHnj7chhl0JcnIrHjno1wiWtgfi8eWVK_7OQAmBHrJWvORFVM2PmrE7AcWZGh-Lj0FvptVvLiUPnCdG_XhNhOov9wQ1fzv7nw5S5EwSvw6CDQNfnMwUAP0XQyQpj70nkx3Nn3p5NFY9IshbNWepKi8ublWFiSPkC0ee8El75Dv5aOxqZQBScbWpWn0Pe2wb6aM1p4Eea_0G00)

[Edit Diagram](http://www.plantuml.com/plantuml/form?url=http://www.plantuml.com/plantuml/png/fLCxJyCm4DxzAsuib4P914i69De1CS38Vd6kYIN7ZcodK8aVp-KX6Y2fKCbY9NV-7lVb2WoOeoVOMRDNfH0lz1vUoNbbpGwrR3K6ws1p3rlk-bN8u972f2AC3GHEbLN8m1D1Jjg-mPuXAZvx9kL1ZW1KY5dOZczMI0Pf54VnHtf7jpaAWJg0sW-uXw4PK3Eb1sMaevfCW6i1_0m6po1l7HfPJUxvu5XYUOHLWq5MLptCudmMK9--u5glJ0dIEaVo1Dw3JgVM6Km4cM9mzyrQXHuQHnj7chhl0JcnIrHjno1wiWtgfi8eWVK_7OQAmBHrJWvORFVM2PmrE7AcWZGh-Lj0FvptVvLiUPnCdG_XhNhOov9wQ1fzv7nw5S5EwSvw6CDQNfnMwUAP0XQyQpj70nkx3Nn3p5NFY9IshbNWepKi8ublWFiSPkC0ee8El75Dv5aOxqZQBScbWpWn0Pe2wb6aM1p4Eea_0G00)

**\*** Some strategies allow the version to be incremented, others don't. More
info below.
**+** This version is out of context with the rest of the example. It is here
simply to show what happens if the check is true.

### Base Version Strategies

Currently we have the following strategies:

- `HighestTagBaseVersionStrategy` - Finds the highest reachable tag from the
current branch
- `VersionInBranchBaseVersionStrategy` - Extracts version information from the
branch name (e.g., `release/3.0.0` will find `3.0.0`)
- `ConfigNextVersionBaseVersionStrategy` - Returns the version from the
GitVersion.yaml file
- `MergeMessageBaseVersionStrategy` - Finds version numbers from merge messages
(e.g., `Merge 'release/3.0.0' into 'master'` will return `3.0.0`)
- `FallbackBaseVersionStrategy` - Always returns 0.1.0 for new repositories

Each strategy needs to return an instance of `BaseVersion` which has the
following properties:

- `Source` - Description of the source (e.g., `Merge message 'Merge 'release/3.0.0' into 'master'`)
- `ShouldIncrement` - Some strategies should have the version incremented,
others do not (e.g., `ConfigNextVersionBaseVersionStrategy` returns false,
`HighestTagBaseVersionStrategy` returns true)
- `SemanticVersion` - SemVer of the base version strategy
- `BaseVersionSource` - SHA hash of the source. Commits will be counted from
this hash. Can be null (e.g., `ConfigNextVersionBaseVersionStrategy` returns
null).
- `BranchNameOverride` - When `useBranchName` or `{BranchName}` is used in the
tag configuration, this allows the branch name to be changed by a base version.
`VersionInBranchBaseVersionStrategy` uses this to strip out anything before the
first `-` or `/.` so `foo` ends up being evaluated as `foo`. If in doubt, just
use null.
