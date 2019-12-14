---
Order: 10
Title: Usage
---

There are two main ways to consume GitVersion, the first is by running
GitVersion.exe. The second is an MSBuild task. The MSBuild task is really easy
to get up and running, simply install GitVersionTask from NuGet and it will
integrate into your project and write out variables to your build server if it's
running on one. The exe offers more options and works for not just .net projects.

- [Using the Command Line tool](command-line)
- [Using the MSBuild Task](msbuild-task)
- [Using the NuGet Library package](nuget-library)
- [Using the Ruby Gem](gem)

 There exist also wrappers around the [Command Line tool](command-line) for
 several build servers. See [Build server support](../build-server-support/build-server-support)
 for details.
