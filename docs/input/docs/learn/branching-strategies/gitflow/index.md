---
Order: 20
Title: Git Flow
Description: The Git Flow branching strategy allows for more structured releases
RedirectFrom: docs/git-branching-strategies/gitflow
---

Git Flow allows more structured releases, and GitVersion will derive sensible
SemVer compatible versions from this structure.

## Assumptions:

* Using [GitFlow branching model][gitflow-branching-model] which always has a
  main and a develop branch
* Following [Semantic Versioning][semantic-versioning]
* Planned releases (bumps in major or minor) are done on release branches
  prefixed with release-. Eg: release-4.1 (or release-4.1.0)
* Hotfixes are prefixed with hotfix- Eg. hotfix-4.0.4
* The original [GitFlow model][gitflow-model]
  specifies branches with a "-" separator while the [git flow extensions][git-flow-extensions]
  default to a "/" separator.  Either work with GitVersion.
* Tags are used on the main branch and reflects the SemVer of each stable
  release eg 3.3.8 , 4.0.0, etc
* Tags can also be used to override versions while we transition repositories
  over to GitVersion
* Using a build server with multi-branch building enabled eg TeamCity 8

## How Branches are handled

The descriptions of how commits and branches are versioned can be considered a
type of pseudopod. With that in mind there are a few common "variables" that we
will refer to:

* `targetBranch` => the branch we are targeting
* `targetCommit` => the commit we are targeting on `targetbranch`

### Main branch

Commits on main will always be a merge commit (Either from a `hotfix` or a
`release` branch) or a tag. As such we can simply take the commit message or tag
message.

If we try to build from a commit that is no merge and no tag then assume `0.1.0`

`mergeVersion` => the SemVer extracted from `targetCommit.Message`

* major: `mergeVersion.Major`
* minor: `mergeVersion.Minor`
* patch: `mergeVersion.Patch`
* pre-release: 0 (perhaps count ahead commits later)
* stability: final

Optional Tags (only when transitioning existing repository):

* TagOnHeadCommit.Name={semver} => overrides the version to be {semver}

Long version:

```txt
{major}.{minor}.{patch} Sha:'{sha}'
1.2.3 Sha:'a682956dccae752aa24597a0f5cd939f93614509'
```

### Develop branch

`targetCommitDate` => the date of the `targetCommit`
`mainVersionCommit` => the first version (merge commit or SemVer tag) on
`main` that is older than the `targetCommitDate`
`mainMergeVersion` => the SemVer extracted from `mainVersionCommit.Message`

* major: `mainMergeVersion.Major`
* minor: `mainMergeVersion.Minor + 1` (0 if the override above is used)
* patch: 0
* pre-release: `alpha.{n}` where n = how many commits `develop` is in front of
  `mainVersionCommit.Date` ('0' padded to 4 characters)

Long version:

```txt
{major}.{minor}.{patch}-{pre-release} Branch:'{branchName}' Sha:'{sha}'
1.2.3-alpha.645 Branch:'develop' Sha:'a682956dccae752aa24597a0f5cd939f93614509'
```

### Hotfix branches

Named: `hotfix-{versionNumber}` eg `hotfix-1.2`

`branchVersion` => the SemVer extracted from `targetBranch.Name`

* major: `mergeVersion.Major`
* minor: `mergeVersion.Minor`
* patch: `mergeVersion.Patch`
* pre-release: `beta{n}` where n = number of commits on branch  ('0' padded to
  4 characters)

Long version:

```txt
{major}.{minor}.{patch}-{pre-release} Branch:'{branchName}' Sha:'{sha}'
1.2.3-beta645 Branch:'hotfix-foo' Sha:'a682956dccae752aa24597a0f5cd939f93614509'
```

### Release branches

* May branch off from: develop
* Must merge back into: develop and main
* Branch naming convention: `release-{n}` eg `release-1.2`

`releaseVersion` => the SemVer extracted from `targetBranch.Name`
`releaseTag` => the first version tag placed on the branch. Note that at least
one version tag is required on the branch. The recommended initial tag is
`{releaseVersion}.0-alpha1`. So for a branch named `release-1.2` the recommended
tag would be `1.2.0-alpha1`

* major: `mergeVersion.Major`
* minor: `mergeVersion.Minor`
* patch: 0
* pre-release: `{releaseTag.preRelease}.{n}` where n = 1 + the number of commits
  since `releaseTag`.

So on a branch named `release-1.2` with a tag `1.2.0-alpha1` and 4 commits after
that tag the version would be `1.2.0-alpha1.4`

Long version:

```txt
{major}.{minor}.{patch}-{pre-release} Branch:'{branchName}' Sha:'{sha}'
1.2.3-alpha2.4 Branch:'release-1.2' Sha:'a682956dccae752aa24597a0f5cd939f93614509'
1.2.3-rc2 Branch:'release-1.2' Sha:'a682956dccae752aa24597a0f5cd939f93614509'
```

### Feature branches

May branch off from: `develop`
Must merge back into: `develop`
Branch naming convention: anything except `main`, `develop`, `release-{n}`, or
`hotfix-{n}`.

TODO: feature branches cannot start with a SemVer. to stop people from create
branches named like "4.0.3"

* major: `mainMergeVersion.Major`
* minor: `mainMergeVersion.Minor + 1` (0 if the override above is used)
* patch: 0
* pre-release: `alpha.feature-{n}` where n = First 8 characters of the commit
  SHA of the first commit

Long version:

```txt
{major}.{minor}.{patch}-{pre-release} Branch:'{branchName}' Sha:'{sha}'
1.2.3-alpha.feature-a682956d Branch:'feature1' Sha:'a682956dccae752aa24597a0f5cd939f93614509'
```

### Pull-request branches

May branch off from: `develop`
Must merge back into: `develop`
Branch naming convention: anything except `main`, `develop`, `release-{n}`, or
`hotfix-{n}`. Canonical branch name contains `/pull/`.

* major: `mainMergeVersion.Major`
* minor: `mainMergeVersion.Minor + 1` (0 if the override above is used)
* patch: 0
* pre-release: `alpha.pull{n}` where n = the pull request number  ('0' padded to
  4 characters)

## Support merges with Mainline

The `GitFlow/v1` preset can use Mainline to calculate versions from history:

```yaml
workflow: GitFlow/v1
calculation:
  strategies:
    - Mainline
```

Mainline supports merging any pair of branches marked `is-main-branch: true`,
including main/support, two support branches, and custom configuration entries.
Each branch's version is calculated independently, using its own ordered
increments and eligible tags. The higher numeric version wins. Neither the
`main` configuration key nor the direction of the merge gives a branch priority.
Older support-line increments are not replayed onto a newer main version.
Repeated merges and round trips compare versions without adding work twice.

For an untagged history with two commits on main followed by two commits on
support, merging support into main produces `0.0.4-3` in ContinuousDelivery or
`0.0.4` in ContinuousDeployment. Tagging the second support commit `0.0.4`
preserves that result. A further support commit after the tag advances the merged
version to `0.0.5-4` or `0.0.5`, respectively. The counting source remains main's
pre-merge tip; the distance includes the new support commits and the merge commit.

If main advances one commit after the fork while support advances three commits,
the numeric result is `0.0.5`, with or without a `0.0.5` tag on support's tip.
The existing counting rule gives `0.0.5-4` in ContinuousDelivery and `0.0.5` in
ContinuousDeployment. This numeric comparison does not change the counting source
or prerelease suffix calculation.

For example, branch from `1.0.0`, tag support at `1.0.5`, tag main at `2.0.0`,
then merge support into main with `git merge --no-ff support/1.x`. With default
merge-prevention settings, the core version is `2.0.0`. The default
ContinuousDelivery mode gives `2.0.0-2`; ContinuousDeployment gives `2.0.0`.
Main's `2.0.0` tag supplies the semantic and counting sources. The two commits
beyond that counting source are support's commit and the merge commit.

The receiver's `prevent-increment.of-merged-branch` suppresses its own branch
increment at the merge; GitFlow enables this for both main and support. The
source's `prevent-increment.when-branch-merged` suppresses its calculated
contribution. Enabled bump messages on the merge itself still apply to the
receiver's candidate before comparison. Source increments are already included
in its independent version and are not applied again to the receiver. Eligible
source tags normally consume the tip's pending increment and bump message;
`prevent-increment.when-current-commit-tagged: false` retains them. The shared
final tag floor can still select a higher reachable tag when source increments
are prevented.

Reversing the tagged example also gives core version `2.0.0`, or `2.0.0-2` in
ContinuousDelivery. Main's higher tag supplies the semantic version while
support's pre-merge tip remains the counting source. The distance is two in
either direction. Equal numeric candidates keep the receiver's semantic source.
Counting source selection and deployment-mode processing are independent of
which branch supplies the higher numeric version.

Branch regex patterns can match names such as `trunk` and `maintenance/1.x`.
Deleting a source ref still works if the merge message identifies the source
and merge-message tracking is enabled. Merges with multiple source parents
remain unsupported. The [regression scenarios][support-merge-tests] assert
versions, source metadata, increment controls, and repeated/nested histories.

## Nightly Builds

**develop**, **feature** and **pull-request** builds are considered nightly
builds and as such are not in strict adherence to SemVer.

## Release Candidates

How do we do release candidates?? Perhaps  tag a release branch and then count
commits forward from the tag to get RC1, RC2 etc??

[gitflow-branching-model]: https://nvie.com/git-model/

[semantic-versioning]: https://semver.org/

[gitflow-model]: https://nvie.com/posts/a-successful-git-branching-model/

[git-flow-extensions]: https://github.com/CJ-Systems/gitflow-cjs

[support-merge-tests]: https://github.com/GitTools/GitVersion/blob/main/src/GitVersion.Core.Tests/IntegrationTests/MainlineSupportMergeScenarios.cs
