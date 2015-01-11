![Icon](https://raw.github.com/Particular/GitVersion/master/Icons/package_icon.png)

The easy way to use semantic versioning (semver.org) with a Git.

GitVersion uses your *git* repository branching conventions to determine the current [Semantic Version](http://semver.org) of your application. It supports [GitFlow](https://github.com/Particular/GitVersion/wiki/GitFlow) and the much simpler [GitHubFlow](https://github.com/Particular/GitVersion/wiki/GitHubFlow) and might work with others (let us know).

GitVersion in action!

![README](Icons/README.png)

You are seeing:

 - Pull requests being built as pre-release builds
 - A branch called `release-1.0.0` producing beta v1 packages

Once a release is done, and you **tag** the commit which was released, the version will automatically be bumped. See our wiki for the rules and examples of how we increment the SemVer.

## Usage:

GitVersion can be used in several ways

 - [An MSBuild Task](https://github.com/Particular/GitVersion/wiki/MSBuild-Task-Usage)
 - [A NuGet Library package](https://github.com/Particular/GitVersion/wiki/GitVersion-NuGet-Library) - to use from your own code
 - [A Command Line tool](https://github.com/Particular/GitVersion/wiki/Command-Line-Tool)
 - [A Ruby Gem](https://github.com/Particular/GitVersion/wiki/Ruby-Gem)

### Examples
We have a bunch of examples in our Wiki, if something is missing, let us know! There are examples for GitHubFlow and GitFlow

### Supported Variables
Because not everyone is the same, we give you a bunch of different variables which you can use in your builds to meet *your* requirements

### Who is using GitVersion?
Find a list of projects who are currently using GitVersion [here](https://github.com/ParticularLabs/GitVersion/wiki/Who-is-using-GitVersion%3F)

## Additional Links

### [FAQ and Common Problems](https://github.com/Particular/GitVersion/wiki/FAQ)

### [Semantic Versioning](http://semver.org/)

## Chat

Have questions?  Come join in the chat room:

[![Gitter](https://badges.gitter.im/Join Chat.svg)](https://gitter.im/ParticularLabs/GitVersion?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

## Icon

<a href="http://thenounproject.com/noun/tree/#icon-No13389" target="_blank">Tree</a> designed by <a href="http://thenounproject.com/david.chapman" target="_blank">David Chapman</a> from The Noun Project
