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

#### develop: 

* major:  latest(master.tag).Major with an option to override from build property (for major bumps)
* minor: latest(master.tag).Minor +1 (0 if the override above is used)
* patch: 0
* Prerelease: unstable{n} where n = number of commits on the develop branch? TC buildnr?

#### master:

* major = latest(merge(release|tag)) If release: use version.major from branch name, if tag use version.major from tag
* minor = latest(merge(release|tag)) If release: use version.minor from branch name, if tag use version.minor from tag
* patch = latest(merge(release|tag)) If release: use 0, if tag use version.patch + 1 from tag

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

