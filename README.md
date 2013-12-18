![Icon](https://raw.github.com/Particular/GitFlowVersion/master/Icons/package_icon.png)

Use convention to derive a SemVer product version from a GitFlow based repository

## Usage:

GitFlowVersion can be used in several ways

### 1. An MSBuild Task

This will wire GitFlowVersion into the MSBuild pipeline of a project and automatically stamp that assembly with the appropriate SemVer information

Available on [Nuget](https://www.nuget.org) under [GitFlowVersionTask](https://www.nuget.org/packages/GitFlowVersionTask/)

    Install-Package GitFlowVersionTask

### 2. A nuget reference package

This can be used if you want to reference GitFlowVersion and reuse is from .net.

Available on [Nuget](https://www.nuget.org) under [GitFlowVersion](https://www.nuget.org/packages/GitFlowVersion/)

    Install-Package GitFlowVersion

### 3. A command line tool

If you want a command line version installed on your machine then you can use [Chocolatey](http://chocolatey.org) to install GitFlowVersion

Available on [Chocolatey](http://chocolatey.org) under [GitFlowVersionTask](http://chocolatey.org/packages/GitFlowVersion)

    cinst GitFlowVersion

#### Calling convention

```    
GitFlowVersion [path] [/l logFilePath]
        path    The directory containing .git. If not defined current directory is used.
        /l      Path to logfile.
```

#### Output

```
{
  "Major":4,
  "Minor":3,
  "Patch":0,
  "Stability":"Unstable",
  "Suffix":"",
  "LongVersion":"4.3.0-unstable103 Branch:'develop' Sha:'0de44745e2a18a9ed1ed6215dc19c95ff46ec0f5'",
  "NugetVersion":"4.3.0-Unstable0103",
  "ShortVersion":"4.3.0-unstable103",
  "BranchName":"develop",
  "BranchType":"Develop",
  "Sha":"0de44745e2a18a9ed1ed6215dc19c95ff46ec0f5"
}
```

## The Problem

Builds are getting more complex and as we're moving towards scm structure with a lot of fine grained repositories we need to take a convention based approach for our product versioning.

This also have the added benefit of forcing us to follow our branching strategy on all repositories since the build breaks if we don't.

### Assumptions:

* Using the [GitFlow branching model](http://nvie.com/git-model/) which means that we always have a master and a develop branch.
* Following [Semantic Versioning](http://semver.org/)
* Planned releases (bumps in major or minor) are done on release branches prefixed with release-. Eg: release-4.1 (or release-4.1.0)
* Hotfixes are prefixed with hotfix- Eg. hotfix-4.0.4
* The original GitFlow model (http://nvie.com/posts/a-successful-git-branching-model/) specifies branches with a "-" separator while the git flow extensions (https://github.com/nvie/gitflow) default to a "/" separator.  Either work with GitFlowVersion.
* Tags are used on the master branch and reflects the SemVer of each stable release eg 3.3.8 , 4.0.0, etc
* Tags can also be used to override versions while we transition repositories over to GitFlowVersion
* Using a build server with multi-branch building enabled eg TeamCity 8

### How Branches are handled

The descriptions of how commits and branches are versioned can be considered a type of pseudopod. With that in mind there are a few common "variables" that we will refer to:

* `targetBranch` => the branch we are targeting
* `targetCommit` => the commit we are targeting on `targetbranch`

#### Master branch

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
* For the moment you need to promote the `%teamcity.build.vcs.branch.{configurationid}%` build parameter to an environment variable with the same name for pull requests to be handled correctly
* We update the TC build number to the GFV number automatically
* We output the individual values of the GFV version as the build parameter: `GitFlowVersion.*` (Eg: `GitFlowVersion.Major`) if you need access to them in your build script 

### NuGet in TeamCity
* Add dummy [parameter](http://confluence.jetbrains.com/display/TCD8/Configuring+Build+Parameters) to 
the project called `GitFlowVersion.NugetVersion`. If many of your projects uses git-flow and SemVer you
can add the parameter to the "root-project" (TeamCity 8.x+)
* Then setup you nuget pack build set the "version" to `%GitFlowVersion.NugetVersion%`

### When TeamCity -> GitHub can't use https

GitFlowVersion requires the presence of master branch in order to determine the version number.  If TeamCity uses https to clone git repos then GitFlowVersion will pull down master branch for you during the build.

If however your TeamCity uses SSH to clone git repos and https is unavailable then GitFlowVersion will error with a message like

> [GitFlowVersionTask.UpdateAssemblyInfo] Error occurred: GitFlowVersion.MissingBranchException: Could not fetch from 'git@github.dev.xero.com:Xero/Bus.git' since LibGit2 does not support the transport. You have most likely cloned using SSH. If there is a remote branch named 'master' then fetch it manually, otherwise please create a local branch named 'master'. ---> LibGit2Sharp.LibGit2SharpException: An error was raised by libgit2. Category = Net (Error).
This transport isn't implemented. Sorry

You need to create a TeamCity build step before your compile step which manually creates a local master branch which tracks remote master.  Like so (in powershell):

```Powershell   
$branchBeingBuilt = . git symbolic-ref --short -q HEAD  
. git pull 2>&1 | write-host
foreach ($remoteBranch in . git branch -r) {   
  . git checkout $remoteBranch.Trim().Replace("origin/", "") 2>&1 | write-host     
  . git pull 2>&1 | write-host  
}  
. git checkout $branchBeingBuilt 2>&1 | write-host  
exit 0
```

you should get build output like

```
[Step 1/1]: Ensure all branches are available for GitFlowVersion (Powershell) (5s)
[Step 1/1] From file:///C:/BuildAgent2/system/git/git-12345678
[Step 1/1]  * [new branch]      master     -> origin/master
[Step 1/1] Switched to a new branch 'master'
[Step 1/1] Branch master set up to track remote branch master from origin.
[Step 1/1] Switched to branch 'develop'
```
## For reference

### [Semantic Versioning](http://semver.org/)

Given a version number MAJOR.MINOR.PATCH, increment the:

 * MAJOR version when you make incompatible API changes,
 * MINOR version when you add functionality in a backwards-compatible manner, and
 * PATCH version when you make backwards-compatible bug fixes.

Additional labels for pre-release and build metadata are available as extensions to the MAJOR.MINOR.PATCH format.
 
###[GitFlow: A successful Git branching model](http://nvie.com/git-model/)
 
![GitFlow](http://nvie.com/img/2009/12/Screen-shot-2009-12-24-at-11.32.03.png)

## Icon

<a href="http://thenounproject.com/noun/tree/#icon-No13389" target="_blank">Tree</a> designed by <a href="http://thenounproject.com/david.chapman" target="_blank">David Chapman</a> from The Noun Project
