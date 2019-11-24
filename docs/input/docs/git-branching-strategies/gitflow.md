#GitFlow
GitFlow allows more structured releases, and GitVersion will derive sensible SemVer compatible versions from this structure.

## Resources

## Assumptions:

* Using [GitFlow branching model](http://nvie.com/git-model/) which always has a master and a develop branch
* Following [Semantic Versioning](http://semver.org/)
* Planned releases (bumps in major or minor) are done on release branches prefixed with release-. Eg: release-4.1 (or release-4.1.0)
* Hotfixes are prefixed with hotfix- Eg. hotfix-4.0.4
* The original GitFlow model (http://nvie.com/posts/a-successful-git-branching-model/) specifies branches with a "-" separator while the git flow extensions (https://github.com/nvie/gitflow) default to a "/" separator.  Either work with GitVersion.
* Tags are used on the master branch and reflects the SemVer of each stable release eg 3.3.8 , 4.0.0, etc
* Tags can also be used to override versions while we transition repositories over to GitVersion
* Using a build server with multi-branch building enabled eg TeamCity 8

## How Branches are handled

The descriptions of how commits and branches are versioned can be considered a type of pseudopod. With that in mind there are a few common "variables" that we will refer to:

* `targetBranch` => the branch we are targeting
* `targetCommit` => the commit we are targeting on `targetbranch`

### Master branch

Commits on master will always be a merge commit (Either from a `hotfix` or a `release` branch) or a tag. As such we can simply take the commit message or tag message.

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

    {major}.{minor}.{patch} Sha:'{sha}'
    1.2.3 Sha:'a682956dccae752aa24597a0f5cd939f93614509'

### Develop branch

`targetCommitDate` => the date of the `targetCommit`
`masterVersionCommit` => the first version (merge commit or SemVer tag) on `master` that is older than the `targetCommitDate`
`masterMergeVersion` => the SemVer extracted from `masterVersionCommit.Message`  

* major: `masterMergeVersion.Major`
* minor: `masterMergeVersion.Minor + 1` (0 if the override above is used)
* patch: 0
* pre-release: `alpha.{n}` where n = how many commits `develop` is in front of `masterVersionCommit.Date` ('0' padded to 4 characters)

Long version:  

    {major}.{minor}.{patch}-{pre-release} Branch:'{branchName}' Sha:'{sha}'
    1.2.3-alpha.645 Branch:'develop' Sha:'a682956dccae752aa24597a0f5cd939f93614509'

### Hotfix branches

Named: `hotfix-{versionNumber}` eg `hotfix-1.2`

`branchVersion` => the SemVer extracted from `targetBranch.Name`  

* major: `mergeVersion.Major`
* minor: `mergeVersion.Minor`
* patch: `mergeVersion.Patch`
* pre-release: `beta{n}` where n = number of commits on branch  ('0' padded to 4 characters)

Long version:  

    {major}.{minor}.{patch}-{pre-release} Branch:'{branchName}' Sha:'{sha}'
    1.2.3-beta645 Branch:'hotfix-foo' Sha:'a682956dccae752aa24597a0f5cd939f93614509'

### Release branches

 * May branch off from: develop
 * Must merge back into: develop and master
 * Branch naming convention: `release-{n}` eg `release-1.2`

`releaseVersion` => the SemVer extracted from `targetBranch.Name`
`releaseTag` => the first version tag placed on the branch. Note that at least one version tag is required on the branch. The recommended initial tag is `{releaseVersion}.0-alpha1`. So for a branch named `release-1.2` the recommended tag would be `1.2.0-alpha1`

* major: `mergeVersion.Major`
* minor: `mergeVersion.Minor`
* patch: 0
* pre-release: `{releaseTag.preRelease}.{n}` where n = 1 + the number of commits since `releaseTag`.

So on a branch named `release-1.2` with a tag `1.2.0-alpha1` and 4 commits after that tag the version would be `1.2.0-alpha1.4`

Long version:  

    {major}.{minor}.{patch}-{pre-release} Branch:'{branchName}' Sha:'{sha}'
    1.2.3-alpha2.4 Branch:'release-1.2' Sha:'a682956dccae752aa24597a0f5cd939f93614509'
    1.2.3-rc2 Branch:'release-1.2' Sha:'a682956dccae752aa24597a0f5cd939f93614509'

### Feature branches

May branch off from: `develop`
Must merge back into: `develop`
Branch naming convention: anything except `master`, `develop`, `release-{n}`, or `hotfix-{n}`.

TODO: feature branches cannot start with a SemVer. to stop people from create branches named like "4.0.3"

* major: `masterMergeVersion.Major`
* minor: `masterMergeVersion.Minor + 1` (0 if the override above is used)
* patch: 0
* pre-release: `alpha.feature-{n}` where n = First 8 characters of the commit SHA of the first commit


Long version:  

    {major}.{minor}.{patch}-{pre-release} Branch:'{branchName}' Sha:'{sha}'
    1.2.3-alpha.feature-a682956d Branch:'feature1' Sha:'a682956dccae752aa24597a0f5cd939f93614509'

### Pull-request branches

May branch off from: `develop`
Must merge back into: `develop`
Branch naming convention: anything except `master`, `develop`, `release-{n}`, or `hotfix-{n}`. Canonical branch name contains `/pull/`.

* major: `masterMergeVersion.Major`
* minor: `masterMergeVersion.Minor + 1` (0 if the override above is used)
* patch: 0
* pre-release: `alpha.pull{n}` where n = the pull request number  ('0' padded to 4 characters)

## Nightly Builds

**develop**, **feature** and **pull-request** builds are considered nightly builds and as such are not in strict adherence to SemVer.

## Release Candidates

How do we do release candidates?? Perhaps  tag a release branch and then count commits forward from the tag to get RC1, RC2 etc??
