![Icon](https://raw.github.com/Particular/GitVersion/master/Icons/package_icon.png)

The easy way to use semantic versioning (semver.org) with a Git.

*GitHubFlowVersion* will automatically version your application to SemVer of  `{vLast.Major}.{vLast.Minor}.{vLast.Patch+1}` where vLast is the last **Git Tag** in your repo and furthermore, it supports detection and versioning of **Pull Requests** if you are using GitHub or Stash.

This means your versions are based on **source control metadata** making it repeatable. *GitVersion* gives you flexibility by making variables available to your build so you can meet all your versioning requirements. 

It also means that unlike many other versioning strategies **you do not have to rebuild your project to bump the version!**

## Supported Branching Models
GitVersion supports both the GitFlow branching model and GitHubFlow. To read more about each of these check out our Wiki:

### GitFlow
Triggered when repository has a `develop` branch

 - [GitFlow: A successful Git branching model](http://nvie.com/git-model/)
 - [GitVersion GitFlow support wiki](https://github.com/Particular/GitVersion/wiki/GitFlow)
 - [Examples](https://github.com/Particular/GitVersion/wiki/GitFlowExamples)

### GitHubFlow
Used when repository has a `master` branch without `develop`

 - [GitHubFlow branching strategy](http://guides.github.com/overviews/flow/) ([Original blog post](http://scottchacon.com/2011/08/31/github-flow.html))
 - [GitVersion GitHubFlow support wiki](https://github.com/Particular/GitVersion/wiki/GitHubFlow)
 - [Examples](https://github.com/Particular/GitVersion/wiki/GitHubFlowExamples)

Need help deciding on which branching model to choose, [click here](https://github.com/Particular/GitVersion/wiki/Choosing-branching-strategy)

## Usage:

GitVersion can be used in several ways

### 1. An MSBuild Task

This will wire GitVersion into the MSBuild pipeline of a project and automatically stamp that assembly with the appropriate SemVer information

Available on [Nuget](https://www.nuget.org) under [GitVersionTask](https://www.nuget.org/packages/GitVersionTask/)

    Install-Package GitVersionTask

### 2. A nuget reference package

This library can be used if you want to reference GitVersion and use it programatically from .net.

Available on [Nuget](https://www.nuget.org) under [GitVersion](https://www.nuget.org/packages/GitVersion/)

    Install-Package GitVersion

### 3. A command line tool

If you want a command line version installed on your machine then you can use [Chocolatey](http://chocolatey.org) to install GitVersion

Available on [Chocolatey](http://chocolatey.org) under [GitVersion](http://chocolatey.org/packages/GitVersion)

    cinst GitVersion

#### Features
The command line too makes variables available for you to use, we currently support:

 - Output variables to your build server for use in your build
 - Return Json object to caller with variables via StdOut ([Example](https://github.com/Particular/GitVersion/blob/master/Tests/JsonVersionBuilderTests.Json.approved.txt))
 - Execute your build script (msbuild) with variables passed as properties
 - Execute an executable with variables available as Environmental Variables to the process

### 4. A ruby gem
If you want a ruby gem version installed on your machine then you can use [Bundler](http://bundler.io/) or Gem(http://rubygems.org/) to install GitVersion

	gem install GitVersion

The calling conventions and the output are the same as the command line version.	

## Supported Variables
Because not everyone is the same, we give you a bunch of different variables which you can use in your builds to meet *your* requirements

Examples assume 1.2.3 has been tagged 3 commits ago, we are build branch `Foo` which is a pull request (#5)

 - *Major*, *Minor*, *Patch*, *Tag* and *Build MetaData* (Build metadata is number of builds since last tag) - `1`, `2`, `3`, `PullRequest5`, `3`
 - *FullSemVer* - The FULL SemVer including tag and build metadata `1.2.3-PullRequest5+3`
 - *SemVer* - The SemVer without build metadata `1.2.3-PullRequest5`
 - *AssemblySemVer* - SemVer with a 0 as the build in the assembly version `1.2.3.0`
 - *ClassicVersion* - SemVer with the build metadata as build number `1.2.3.3`
 - *BranchName* - The branch name `Foo`
 - *Sha* - Git sha of HEAD

## The Problem

Builds are getting more complex and as we're moving towards scm structure with a lot of fine grained repositories we need to take a convention based approach for our product versioning.

This also have the added benefit of forcing us to follow our branching strategy on all repositories since the build breaks if we don't.

## Additional Links

### [FAQ and Common Problems](https://github.com/Particular/GitVersion/wiki/FAQ)

### [Semantic Versioning](http://semver.org/)

Given a version number MAJOR.MINOR.PATCH, increment the:

 * MAJOR version when you make incompatible API changes,
 * MINOR version when you add functionality in a backwards-compatible manner, and
 * PATCH version when you make backwards-compatible bug fixes.

Additional labels for pre-release and build metadata are available as extensions to the MAJOR.MINOR.PATCH format.

## Icon

<a href="http://thenounproject.com/noun/tree/#icon-No13389" target="_blank">Tree</a> designed by <a href="http://thenounproject.com/david.chapman" target="_blank">David Chapman</a> from The Noun Project
