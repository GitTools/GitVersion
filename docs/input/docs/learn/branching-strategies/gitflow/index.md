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

Mainline supports merging a configured `support` branch into the configured
`main` branch. It keeps main's calculated baseline and treats the newly merged
support history as one increment contribution. A lower support tag does not
replace main's baseline. Source history already covered by an eligible tag or
an earlier merge is not replayed as another commit-message bump.

For example, branch from `1.0.0`, tag support at `1.0.5`, tag main at `2.0.0`,
then merge support into main with `git merge --no-ff support/1.x`. With default
Patch increments, the core version is `2.0.1`. The default ContinuousDelivery
mode gives `2.0.1-2`; explicitly selecting ContinuousDeployment gives `2.0.1`.
Main's `2.0.0` tag supplies the semantic and counting sources. The two commits
beyond that counting source are support's commit and the merge commit.

The support increment and its enabled commit-message rules determine the
contribution. Main's `prevent-increment.of-merged-branch` suppresses main's own
branch increment at this merge; support's `prevent-increment.when-branch-merged`
suppresses the support contribution. Enabled bump messages on the merge itself
still apply. Both numeric version selection and the existing final tag floor
remain active: a higher eligible support tag can raise the final numeric version.

The reverse direction, merging the configured main branch into support, uses
the existing recursive Mainline calculation for main's history. For the same
tagged graph (`2.0.0` on main and `1.0.5` on support), the result is also core
version `2.0.1`, or `2.0.1-2` in ContinuousDelivery. Main's tag supplies both
sources and the counting distance is two. Eligible source tags consume their
commit-message bumps by default. Setting main's
`prevent-increment.when-current-commit-tagged` to `false` keeps those bumps
active. Newer source commits and enabled merge-message bumps still apply.
Support's `prevent-increment.of-merged-branch` suppresses its own
increment, and main's `prevent-increment.when-branch-merged` suppresses main's
pending increment. A reset message on main suppresses pending increments, but
does not undo numeric version increments already calculated for earlier main
commits.

Both directions recognize the configured branch patterns and work after
deleting the source ref, provided the merge message still identifies it.
Arbitrary merges between other distinct main branches, including two support
branches, remain unsupported. The forward path batches support's contribution;
the reverse path recursively calculates main's history. This is partial
support for multi-mainline histories. The [regression scenarios][support-merge-tests]
assert versions, source metadata, increment controls, and the scope limits.

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
