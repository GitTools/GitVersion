

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

#### develop

* major:  latest(master.tag).Major with an option to override from build property (for major bumps)
* minor: latest(master.tag).Minor +1 (0 if the override above is used)
* patch: 0
* pre-release: unstable{n} where n = number of commits on the develop branch? TC builder?

#### master

Commits on master will always be a merge commit. Either from a `hotfix` or a `release` branch. As such we can simply take the commit message.

* major = take first part of version from commit message
* minor = take second part of version from commit message
* patch = take third part of version from commit message

#### hotfix branches

* major = semver(branch name without prefix).major
* minor= semver(branch name without prefix).minor
* patch= semver(branch name without prefix).patch
* prerelease: beta{number of commits on branch}

#### release branches

* major = semver(branch name without prefix).major
* minor= semver(branch name without prefix).minor
* patch = 0
* prerelease: beta{number of commits on branch}


## Repository to test

So this project assumes you have https://github.com/Particular/GitFlow checked out to the same directory for running unit tests

## For reference

### [Semantic Versioning](http://semver.org/)

Given a version number MAJOR.MINOR.PATCH, increment the:

MAJOR version when you make incompatible API changes,
MINOR version when you add functionality in a backwards-compatible manner, and
PATCH version when you make backwards-compatible bug fixes.
Additional labels for pre-release and build metadata are available as extensions to the MAJOR.MINOR.PATCH format.
 
###[GitFlow: A successful Git branching model](http://nvie.com/git-model/)
 
![](http://nvie.com/img/2009/12/Screen-shot-2009-12-24-at-11.32.03.png)