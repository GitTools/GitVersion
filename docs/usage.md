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
The MSBuild task will wire GitVersion into the MSBuild pipeline of a project and automatically stamp that assembly with the appropriate SemVer information

Available on [Nuget](https://www.nuget.org) under [GitVersionTask](https://www.nuget.org/packages/GitVersionTask/)

    Install-Package GitVersionTask

Remove the `Assembly*Version` attributes from your `Properties\AssemblyInfo.cs` file. Sample default:

    [assembly: AssemblyVersion("1.0.0.0")]
    [assembly: AssemblyFileVersion("1.0.0.0")]
    [assembly: AssemblyInformationalVersion("1.0.0.0")]

Make sure there is a tag somewhere on master named `v1.2.3` before `HEAD` (change the numbers as desired).  Now when you build:

* AssemblyVersion will be set to 1.2.0.0 (i.e Major.Minor.0.0)
* AssemblyFileVersion will be set to 1.2.3.0 (i.e Major.Minor.Patch)
* AssemblyInformationalVersion will be set to `1.2.4+<commitcount>.Branch.<branchname>.Sha.<commithash>` where:
    * `<commitcount>` is the number of commits between the `v1.2.3` tag and `HEAD`.
    * `<branchname>` is the name of the branch you are on.
    * `<commithash>` is the commit hash of `HEAD`.

Continue working as usual and when you release/deploy, tag the branch/release `v1.2.4`.

If you want to bump up the major or minor version, create a `GitVersionConfig.yaml` file in the root of your repo and inside of it on a single line enter `next-version: <version you want>`, for example `next-version: 3.0.0`

### Why is AssemblyVersion only set to Major.Minor?

This is a common approach that gives you the ability to roll out hot fixes to your assembly without breaking existing applications that may be referencing it. You are still able to get the full version number if you need to by looking at its file version number.

### My Git repository requires authentication. What do I do?

Set the environmental variables `GITVERSION_REMOTE_USERNAME` and `GITVERSION_REMOTE_PASSWORD` before the build is initiated.

## NuGet Library
To use GitVersion from your own code.

**Warning, we are not semantically versioning this library and it should be considered unstable.**

It also is not currently documented. Please open an issue if you are consuming the library to let us know, and why you need to.

## Gem
Just a gem wrapper around the command line to make it easier to consume from Rake.

**NOTE** This is currenly not being pushed.. Please get in touch if you are using this

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
