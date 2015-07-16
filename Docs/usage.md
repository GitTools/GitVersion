# Usage
There are two main ways to consume GitVersion, the first is by running GitVersion.exe. The second is an MSBuild task. The MSBuild task is really easy to get up and running, simply install GitVersionTask from NuGet and it will integrate into your project and write out variables to your build server if it's running on one. The exe offers more options and works for not just .net projects.

 - [A Command Line tool](usage/commandLine.md)
 - [An MSBuild Task](usage/msbuildTask.md)

## Other options
 - [A NuGet Library package](https://github.com/Particular/GitVersion/wiki/GitVersion-NuGet-Library) - to use from your own code. Warning, we are not semantically versioning this library and it should be considered unstable.
 - [A Ruby Gem](https://github.com/Particular/GitVersion/wiki/Ruby-Gem) - just a gem wrapper around the command line to make it easier to consume from Rake
