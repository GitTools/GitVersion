![Icon](https://raw.github.com/Particular/GitFlowVersion/master/Icons/package_icon.png)

## Usage:

    Install-Package GitFlowVersionTask

## The Problem

Our builds are getting more complex and as we're moving towards scm structure with a lot of fine grained repos we need to take a convention based approach for our assembly versioning.

This also have the added benefit of forcing us to follow our branching strategy on all repositories since the build breaks if we don't.

### Assumptions:

* We use the git flow branching strategy which means that we always have a master and a develop branch.
* Planned releases (bumps in major or minor) are done on release branches prefixed with release-. Eg: release-4.1 (or release-4.1.0)
* Hotfixes are prefixed with hotfix- Eg. hotfix-4.0.4
* Tags are used on the master branch and reflects the SemVer of each stable release eg 3.3.8 , 4.0.0, etc
* Tags can also be used to override versions while we transition repos over to GitFlowVersion
* Using a build server with multi-branch building enabled eg TeamCity 8

### Suggested conventions

`targetBranch` => the branch we are targeting
`targetCommit` => the commit we are targeting on `targetbranch`

#### Master branch

Commits on master will always be a merge commit (Either from a `hotfix` or a `release` branch) or a tag. As such we can simply take the commit message or tag message.

If we try to build from a commit that is no merge and no tag then assume `0.1.0`

`mergeVersion` => the SemVer extracted from `targetCommit.Message`  
 
* major: `mergeVersion.Major`
* minor: `mergeVersion.Minor`
* patch: `mergeVersion.Patch`
* pre-release: 0 (perhaps count ahead commits later)
* stability: final

Optional Tags (only when transitioning existing repos): 
* TagOnHeadCommit.Name={semver} => overrides the version to be {semver} 

Long version:  

    {major}.{minor}.{patch} Sha:'{sha}'
    1.2.3 Sha:'a682956dccae752aa24597a0f5cd939f93614509'

#### Develop branch

`targetCommitDate` => the date of the `targetCommit`
`masterVersionCommit` => the first version (merge commit or SemVer tag) on `master` that is older than the `targetCommitDate`
`masterMergeVersion` => the SemVer extracted from `masterVersionCommit.Message`  

* major: `masterMergeVersion.Major`
* minor: `masterMergeVersion.Minor + 1` (0 if the override above is used)
* patch: 0
* pre-release: `unstable{n}` where n = how many commits `develop` is in front of `masterVersionCommit.Date` ('0' padded to 4 characters)

Long version:  

    {major}.{minor}.{patch}-{pre-release} Branch:'{branchName}' Sha:'{sha}'
    1.2.3-unstable645 Branch:'develop' Sha:'a682956dccae752aa24597a0f5cd939f93614509'

#### Hotfix branches

Named: `hotfix-{versionNumber}` eg `hotfix-1.2`

`branchVersion` => the SemVer extracted from `targetBranch.Name`  

* major: `mergeVersion.Major`
* minor: `mergeVersion.Minor`
* patch: `mergeVersion.Patch`
* pre-release: `beta{n}` where n = number of commits on branch  ('0' padded to 4 characters)

Long version:  

    {major}.{minor}.{patch}-{pre-release} Branch:'{branchName}' Sha:'{sha}'
    1.2.3-beta645 Branch:'hotfix-foo' Sha:'a682956dccae752aa24597a0f5cd939f93614509'

#### Release branches

May branch off from: develop
Must merge back into: develop and master
Branch naming convention: `release-{n}`  eg `release-1.2`

`releaseVersion` => the SemVer extracted from `targetBranch.Name`  

* major: `mergeVersion.Major`
* minor: `mergeVersion.Minor`
* patch: 0
* pre-release: `beta{n}` where n = number of commits on branch or  `rc{n}` where n is derived from a tag.

Optional Tags (only when changing pre-release status):

* `TagOnHeadCommit.Name={semver}` => overrides the pre-release part. Eg 1.0.0-RC1 (stable part should match)
 
Long version:  

    {major}.{minor}.{patch}-{pre-release} Branch:'{branchName}' Sha:'{sha}'
    1.2.3-beta2 Branch:'release-1.2' Sha:'a682956dccae752aa24597a0f5cd939f93614509'
    1.2.3-rc2 Branch:'release-1.2' Sha:'a682956dccae752aa24597a0f5cd939f93614509'

#### Feature  branches

May branch off from: `develop`
Must merge back into: `develop`
Branch naming convention: anything except `master`, `develop`, `release-{n}`, or `hotfix-{n}`.

TODO: feature branches cannot start with a SemVer. to stop people from create branches named like "4.0.3"

* major: `masterMergeVersion.Major`
* minor: `masterMergeVersion.Minor + 1` (0 if the override above is used)
* patch: 0
* pre-release: `unstable.feature-{n}` where n = First 8 characters of the commit SHA of the first commit


Long version:  

    {major}.{minor}.{patch}-{pre-release} Branch:'{branchName}' Sha:'{sha}'
    1.2.3-unstable.feature-a682956d Branch:'feature1' Sha:'a682956dccae752aa24597a0f5cd939f93614509'

#### Pull-request  branches

May branch off from: `develop`
Must merge back into: `develop`
Branch naming convention: anything except `master`, `develop`, `release-{n}`, or `hotfix-{n}`. Canonical branch name contains `/pull/`.

* major: `masterMergeVersion.Major`
* minor: `masterMergeVersion.Minor + 1` (0 if the override above is used)
* patch: 0
* pre-release: `unstable.pull{n}` where n = the pull request number  ('0' padded to 4 characters)

### Nightly Builds

**develop**, **feature** and **pull-request** builds are considered nightly builds and as such are not in strict adherence to SemVer. 

## Release Candidates

How do we do release candidates?? Perhaps  tag a release branch and then count commits forward from the tag to get RC1, RC2 etc??

## Running inside TeamCity

* Make sure to use agent checkouts
* For the moment you need to promote the %teamcity.build.vcs.branch.{configurationid}% build parameter to an environment variable with the same name for pull requests to be handled correctly
* We update the TC buildnumber to the GFV number automatically
* We output the individual values of the GFV version as the build parameter: GitFlowVersion.* (Eg: GitFlowVersion.Major) if you need access to them in your build script 


## For reference

### [Semantic Versioning](http://semver.org/)

Given a version number MAJOR.MINOR.PATCH, increment the:

MAJOR version when you make incompatible API changes,
MINOR version when you add functionality in a backwards-compatible manner, and
PATCH version when you make backwards-compatible bug fixes.
Additional labels for pre-release and build metadata are available as extensions to the MAJOR.MINOR.PATCH format.
 
###[GitFlow: A successful Git branching model](http://nvie.com/git-model/)
 
![GitFlow](http://nvie.com/img/2009/12/Screen-shot-2009-12-24-at-11.32.03.png)

## Icon

<a href="http://thenounproject.com/noun/tree/#icon-No13389" target="_blank">Tree</a> designed by <a href="http://thenounproject.com/david.chapman" target="_blank">David Chapman</a> from The Noun Project
