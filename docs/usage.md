# Usage

There are two main ways to consume GitVersion, the first is by running GitVersion.exe. The second is an MSBuild task. The MSBuild task is really easy to get up and running, simply install GitVersionTask from NuGet and it will integrate into your project and write out variables to your build server if it's running on one. The exe offers more options and works for not just .net projects.

 - [A Command Line tool](#command-line)
 - [An MSBuild Task](#msbuild-task)
 - [A NuGet Library package](#nuget-library)
 - [A Ruby Gem](#gem)


## Command Line

If you want a command line version installed on your machine then you can use [Chocolatey](http://chocolatey.org) to install GitVersion

Available on [Chocolatey](http://chocolatey.org) under [GitVersion.Portable](http://chocolatey.org/packages/GitVersion.Portable)

 > choco install GitVersion.Portable

Switches are available with `GitVersion /?`


### Output

By default GitVersion returns a json object to stdout containing all the [variables](more-info/variables.md) which GitVersion generates. This works great if you want to get your build scripts to parse the json object then use the variables, but there is a simpler way.

`GitVersion.exe /output buildserver` will change the mode of GitVersion to write out the variables to whatever build server it is running in. You can then use those variables in your build scripts or run different tools to create versioned NuGet packages or whatever you would like to do. See [build servers](build-server-support.md) for more information about this.


## MSBuild Task

Available on [Nuget](https://www.nuget.org) under [GitVersionTask](https://www.nuget.org/packages/GitVersionTask/)

    Install-Package GitVersionTask

The MSBuild task will wire GitVersion into the MSBuild pipeline of a project and perform several actions.

### 1. Inject version metadata into the assembly 

Task Name: `GitVersionTask.UpdateAssemblyInfo`

#### AssemblyInfo Attributes

At build time a temporary AssemblyInfo.cs will be created that contains the appropriate SemVer information. This will will be included in the build pipeline.

Ensure you remove the `Assembly*Version` attributes from your `Properties\AssemblyInfo.cs` file so as to not get duplicate attribute warnings. Sample default:

    [assembly: AssemblyVersion("1.0.0.0")]
    [assembly: AssemblyFileVersion("1.0.0.0")]
    [assembly: AssemblyInformationalVersion("1.1.0+Branch.master.Sha.722aad3217bd49a6576b6f82f60884e612f9ba58")]

Now when you build:

* `AssemblyVersion` will be set to the `AssemblySemVer` variable.
* `AssemblyFileVersion` will be set to the `MajorMinorPatch` variable with a appended `.0`.
* `AssemblyInformationalVersion` will be set to the `InformationalVersion` variable.


#### Other injected Variables

All other [variables](more-info/variables.md) will be injected into a internal static class.

```
namespace AssemblyName
{
	[CompilerGenerated]
	internal static class GitVersionInformation
	{
		public static string Major = "1";
		public static string Minor = "1";
		public static string Patch = "0";
		...All other variables
	}
}
```


#### Accessing injected Variables

**All variables**

```
var assemblyName = assembly.GetName().Name;
var gitVersionInformationType = assembly.GetType(assemblyName + ".GitVersionInformation");
var fields = gitVersionInformationType.GetFields();

foreach (var field in fields)
{
    Trace.WriteLine(string.Format("{0}: {1}", field.Name, field.GetValue(null)));
}
```

**Specific variable**

```
var assemblyName = assembly.GetName().Name;
var gitVersionInformationType = assembly.GetType(assemblyName + ".GitVersionInformation");
var versionField = gitVersionInformationType.GetField("Major");
Trace.WriteLine(versionField.GetValue(null));
```


### 2. Populate some MSBuild properties with version metadata

Task Name: `GitVersionTask.GetVersion`

At build time all the derived [variables](more-info/variables.md) will be written to MSBuild properties so the information can be used by other tooling in the build pipeline.

The class for `GitVersionTask.GetVersion` has a property for each variable. However at MSBuild time these properties a mapped to MSBuild properties that are prefixed with `GitVersion_`. This prevents conflicts with other properties in the pipeline.

#### Accessing variable in MSBuild

After `GitVersionTask.GetVersion` has executed the properties can be used in the standard way. For example:

    <Message Text="GitVersion_InformationalVersion: $(GitVersion_InformationalVersion)"/> 


### 3. Communicate variables to current Build Server

Task Name: `GitVersionTask.WriteVersionInfoToBuildLog`

If, at build time, it is detected that the build is occurring inside a Build Server server then the [variables](more-info/variables.md) will be written to the build log in a format that the current Build Server can consume. See [Build Server Support](build-server-support.md).

### Conditional control tasks

Properties `WriteVersionInfoToBuildLog`, `UpdateAssemblyInfo` and `GetVersion` are checked before running these tasks.

If you, eg., want to disable `GitVersionTask.UpdateAssemblyInfo` just define `UpdateAssemblyInfo` to something other than `true` in your MSBuild script, like this:

```
  <PropertyGroup>
	...
    <UpdateAssemblyInfo>false</UpdateAssemblyInfo>
	...
  </PropertyGroup>
```
  
### My Git repository requires authentication. What do I do?

Set the environmental variables `GITVERSION_REMOTE_USERNAME` and `GITVERSION_REMOTE_PASSWORD` before the build is initiated.


## NuGet Library

To use GitVersion from your own code.

**Warning, we are not semantically versioning this library and it should be considered unstable.**

It also is not currently documented. Please open an issue if you are consuming the library to let us know, and why you need to.


## Gem

Just a gem wrapper around the command line to make it easier to consume from Rake.

**NOTE** This is currently not being pushed.. Please get in touch if you are using this

If you want a Ruby gem version installed on your machine then you can use [Bundler](http://bundler.io/) or [Gem](http://rubygems.org/) to install the `gitversion` gem.

	gem install gitversion

The gem comes with a module to include in your Rakefile:

```ruby
require 'git_version'

include GitVersion

puts git_version.sha
```

Internally, this will call the `GitVersion.exe` that is bundled with the Ruby gem, parse its JSON output and make all the JSON keys available through Ruby methods. You can either use Pascal case (`git_version.InformationalVersion`) or Ruby-style snake case (`git_version.informational_version`) to access the JSON properties.

gitversion internally caches the JSON output, so `GitVersion.exe` is only called once.

Any arguments passed to `git_version` will be passed to `GitVersion.exe`:

```ruby
require 'git_version'

include GitVersion

puts git_version('C:/read/info/from/another/repository').sha
```

**Note:** Mono is not currently supported due to downstream dependencies on libgit2. The Gem can only be used with the .NET framework
