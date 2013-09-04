![Icon](https://raw.github.com/Particular/GitFlowVersion/master/Icon/package_icon.png)

## The Problem

Our builds are getting more complex and as we're moving towards scm structure with a lot of fine grained repos we need to take a convention based approach for our assembly versioning.

Given that we use the git flow branching strategy, GitHub and team city I suggest the following:

### Assumptions:

* We use the git flow branching strategy which means that we always have a master and a develop branch.
* When feature branches is uses they are prefixed with feature-
* Planned releases (bumps in major or minor) are done on release branches prefixed with release-. Eg: release-4.1
* Hotfixes are prefixed with hotfix- Eg. hotfix-4.0.4
* Tags are used on the master branch and reflects the semver of each stable release eg 3.3.8 , 4.0.0, etc
* We use teamcity 8 for our builds

### Suggested conventions

`targetBranch` => the branch we are targeting
`targetCommit` => the commit we are targeting on `targetbranch`

#### develop

`targetCommitDate` => the date of the `targetCommit`
`masterVersionCommit` => the first version (merge commit or SemVer tag) on `master` that is older than the `targetCommitDate`
`masterMergeVersion` => the SemVer extracted from `masterVersionCommit.Message`  

* major: `masterMergeVersion.Major`
* minor: `masterMergeVersion.Minor + 1` (0 if the override above is used)
* patch: 0
* pre-release: unstable{n} where n = how many commits `develop` is in front of `masterVersionCommit.Date`

#### master

Commits on master will always be a merge commit (Either from a `hotfix` or a `release` branch) or a tag. As such we can simply take the commit message or tag message.

If we try to build from a commit that is not a merge we should throw an `Exception`

`mergeVersion` => the SemVer extracted from `targetCommit.Message`  
 
* major: `mergeVersion.Major`
* minor: `mergeVersion.Minor`
* patch: `mergeVersion.Patch`
* pre-release: 0 (perhaps count ahead commits later)

#### hotfix branches

`branchVersion` => the SemVer extracted from `targetBranch.Name`  

* major: `mergeVersion.Major`
* minor: `mergeVersion.Minor`
* patch: `mergeVersion.Patch`
* pre-release: beta{number of commits on branch}

#### release branches

`releaseVersion` => the SemVer extracted from `targetBranch.Name`  

* major: `mergeVersion.Major`
* minor: `mergeVersion.Minor`
* patch: 0
* pre-release: beta{number of commits on branch}

#### feature  branches

TODO: feature branches cannot start with a semver. to stop people from create branches named like "4.0.3"

* major: `masterMergeVersion.Major`
* minor: `masterMergeVersion.Minor + 1` (0 if the override above is used)
* patch: 0
* pre-release: Feature{First 8 characters of the commit SHA of the first commit}

#### pull-request  branches


* major: `masterMergeVersion.Major`
* minor: `masterMergeVersion.Minor + 1` (0 if the override above is used)
* patch: 0
* pre-release: Pull{pull request no}


## Repository to test

So this project assumes you have https://github.com/Particular/GitFlow checked out to the same directory for running unit tests

## Release Candidates

How do we do release candidates?? Perhaps  tag a release branch and then count forward from the tag to get RC1, RC2 etc??



## For reference

### [Semantic Versioning](http://semver.org/)

Given a version number MAJOR.MINOR.PATCH, increment the:

MAJOR version when you make incompatible API changes,
MINOR version when you add functionality in a backwards-compatible manner, and
PATCH version when you make backwards-compatible bug fixes.
Additional labels for pre-release and build metadata are available as extensions to the MAJOR.MINOR.PATCH format.
 
###[GitFlow: A successful Git branching model](http://nvie.com/git-model/)
 
![](http://nvie.com/img/2009/12/Screen-shot-2009-12-24-at-11.32.03.png)

## Icon

<a href="http://thenounproject.com/noun/tree/#icon-No13389" target="_blank">Tree</a> designed by <a href="http://thenounproject.com/david.chapman" target="_blank">David Chapman</a> from The Noun Project
