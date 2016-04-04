# GitVersion Docs
GitVersion is a tool to help you achieve *Semantic Versioning* on your project.

This influences many of the decisions GitVersion has made, please read and understand this page as it will help you start using GitVersion effectively!

## Assumptions/Rules
### Tags override other rules
If a commit is tagged, then GitVersion will *always* use that version over any calculated versions. This is so if you rebuild a tag then the same version will be produced.

### The Semantic Version does not increment every commit
This trips a lot of people up, by default GitVersion *does not* increment the SemVer every commit. This means that you will get multiple builds producing the *same version* of your application.

Read more at [version increments](more-info/version-increments.md)

### Version sources
There are a number of sources GitVersion can get its versions from, they include:

 - Tags
 - Version numbers in branches (e.g. `release/2.0.0`)
 - Merge messages (for branches with versions in them, e.g. `Merged branch 'release/2.0.0' into master`)
 - Track version of another branch (e.g. develop tracks master, so when master increments so does develop)
 - GitVersion.yml file (e.g `next-version: 2.0.0`)

Read more at [version sources](more-info/version-sources.md)

## Configuration
GitVersion v3 was rewritten to be very configuration driven rather than hardcoding git workflows into it. This has made it a lot more flexible. Configuration options can be set globally or per branch.

Read more about [configuration](configuration.md)

## Output Variables
We recognise that a single formatted version number does not work for all cases. A simple example is NuGet, it doesn't support SemVer 2.0 meaning that the SemVer of `1.3.5-beta.10+500` needs to be formatted as `1.3.5-beta0010` so it will sort properly.

You can just run `GitVersion.exe` in your repository to see what variables are available (by default a json object is returned).

## Exe or MSBuild Task
There are two ways to consume GitVersion, the first is by running GitVersion.exe. The second is an MSBuild task. The MSBuild task is really easy to get up and running, simply install GitVersionTask from NuGet and it will integrate into your project and write out variables to your build server if it's running on one. The exe offers more options and works for .NET projects as well as other project types.

Read more about [using GitVersion](usage/usage.md)

## Supported languages and frameworks
GitVersion is language and framework agnostic.

### Automatic update of version in projects
GitVersion supports the following projects to automatically update its version information.

| ProjectType                           | Command Line Tool           | MS Build Task  |
| :------------------------------------ | :-------------------------- | :------------- |
| C# Project with `AssemblyInfo.cs`     | Supported                   | Supported      |
| VB.NET Project with `AssemblyInfo.vb` | Supported                   | Supported      |
