---
Order: 30
Title: MSBuild Task
Description: |
    Just install with NuGet and GitVersion will automatically generate assembly
    version information that is compiled into the resulting artifact.
CardIcon: collect.svg
RedirectFrom:  docs/usage/msbuild-task
---

The MSBuild Task for GitVersion — **GitVersion.MsBuild** — is a simple solution if
you want to version your assemblies without writing any command line scripts or
modifying your build process.

Just install with NuGet and GitVersion will automatically generate assembly
version information that is compiled into the resulting artifact.

Since version 6.0 only MSBuild running on .NET Core (`dotnet msbuild`) is supported.

Unfortunately, up until at least Visual Studio 2022 17.11, Visual Studio runs all builds
using the .NET Framework version of MSBuild, and therefore **Visual Studio is not supported**. 
For more information see [this discussion](https://github.com/GitTools/GitVersion/discussions/4130).  

## TL;DR

### Install the MSTask targets

Add the [GitVersion.MsBuild](https://www.nuget.org/packages/GitVersion.MsBuild/) NuGet
Package into the project you want to be versioned by GitVersion.

From the Package Manager Console:

```shell
Install-Package GitVersion.MsBuild
```

If you're using `PackageReference` style NuGet dependencies (VS 2017+), add
`<PrivateAssets>all</PrivateAssets>` to prevent the task from becoming a
dependency of your package:

```xml
<PackageReference Include="GitVersion.MsBuild" Version="6.0.0">
  <PrivateAssets>All</PrivateAssets>
</PackageReference>
```

### Remove AssemblyInfo attributes

The next thing you need to do is to remove the `Assembly*Version` attributes from
your `Properties\AssemblyInfo.cs` files. This puts GitVersion.MsBuild in charge of
versioning your assemblies.

### Done!

The setup process is now complete and GitVersion.MsBuild should be working its magic,
versioning your assemblies like a champ. However, more can be done to further
customize the build process. Keep reading to find out how the version variables
are set and how you can use them in MSBuild tasks.

## Configuration

The [configuration file](/docs/reference/configuration), if any, is read from a file
the `GitVersion.yml` the root of the repository or the project directory. Since version 3,
the path to the configuration file itself [cannot be configured](https://github.com/GitTools/GitVersion/issues/3009).

## How does it work?

After being installed into a project, the MSBuild task will wire GitVersion into
the MSBuild pipeline and will then perform several actions. These actions are
described below.

### Inject version metadata into the assembly

The sub-task named `GitVersion.MsBuild.UpdateAssemblyInfo` will inject version
metadata into the assembly where GitVersion.MsBuild has been added to. For each assembly
you want GitVersion to handle versioning, you will need to install
[GitVersion.MsBuild](https://www.nuget.org/packages/GitVersion.MsBuild/) into the corresponding
project via NuGet.

#### AssemblyInfo Attributes

A temporary `AssemblyInfo.cs` will be created at build time. That file will
contain the appropriate SemVer information. This will be included in the build
pipeline.

Default sample:

```csharp
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.1.0+Branch.main.Sha.722aad3217bd49a6576b6f82f60884e612f9ba58")]
```

Now, when you build:

* `AssemblyVersion` will be set to the `AssemblySemVer` variable.
* `AssemblyFileVersion` will be set to the `MajorMinorPatch` variable with `.0`
    appended to it.
* `AssemblyInformationalVersion` will be set to the `InformationalVersion` variable.

#### Other injected Variables

All other [variables](/docs/reference/variables) will be injected into an
internal static class part of the global namespace similar to this:

```csharp
[CompilerGenerated]
internal static class GitVersionInformation
{
    public static string Major = "1";
    public static string Minor = "1";
    public static string Patch = "0";
    ...All other variables
}
```

### Accessing injected Variables

**NB: depending on the source language of the assembly, the injected variables may be exposed either as fields or as properties. The examples below take care of this.**

#### All variables

```csharp
var assemblyName = assembly.GetName().Name;
var gitVersionInformationType = assembly.GetType("GitVersionInformation");
var fields = gitVersionInformationType.GetFields();

foreach (var field in fields)
{
    Trace.WriteLine(string.Format("{0}: {1}", field.Name, field.GetValue(null)));
}

// The GitVersionInformation class generated from a F# project exposes properties
var properties = gitVersionInformationType.GetProperties();

foreach (var property in properties)
{
    Trace.WriteLine(string.Format("{0}: {1}", property.Name, property.GetGetMethod(true).Invoke(null, null)));
}
```

##### Specific variable

```csharp
var assemblyName = assembly.GetName().Name;
var gitVersionInformationType = assembly.GetType("GitVersionInformation");
var versionField = gitVersionInformationType.GetField("Major");
if (versionField != null)
{
    Trace.WriteLine(versionField.GetValue(null));
}
else
{
    // The GitVersionInformation class generated from a F# project exposes properties
    var versionProperty = gitVersionInformationType.GetProperty("Major");
    if (versionProperty != null)
    {
        Trace.WriteLine(versionProperty.GetGetMethod(true).Invoke(null, null));
    }
}
```

### Populate some MSBuild properties with version metadata

The sub-task `GitVersion.MsBuild.GetVersion` will write all the derived
[variables](/docs/reference/variables) to MSBuild properties so the information
can be used by other tooling in the build pipeline.

The class for `GitVersion.MsBuild.GetVersion` has a property for each variable.
However at MSBuild time these properties are mapped to MSBuild properties that
are prefixed with `GitVersion_`. This prevents conflicts with other properties
in the pipeline.

In addition, the following MSBuild properties are set when `UpdateVersionProperties`
is true (the default): `Version`, `VersionPrefix`, `VersionSuffix`,
`PackageVersion`, `InformationalVersion`, `AssemblyVersion` and `FileVersion`.
These are used by the built-in tasks for generating AssemblyInfo's and NuGet
package versions.

### NuGet packages

The new SDK-style projects available for .NET Standard libraries (and multi-targeting),
have the ability to create NuGet packages directly by using the `pack` target:
`msbuild /t:pack`. The version is controlled by the MSBuild properties described
above.

GitVersionTask generates SemVer 2.0 compliant NuGet package versions by default.
You can disable it by setting `UseFullSemVerForNuGet` to false in your project.
Older NuGet clients do not support SemVer 2.0 package versions, but most of the
modern hosts support it.

#### Accessing variables in MSBuild

Once `GitVersion.MsBuild.GetVersion` has been executed, the MSBuild properties can be
used in the standard way. For example:

```xml
<Message Text="GitVersion_InformationalVersion: $(GitVersion_InformationalVersion)"/>
```

### Communicate variables to current Build Server

The sub-task `GitVersion.MsBuild.WriteVersionInfoToBuildLog` will attempt to write
the version information to the current Build Server log.

If, at build time, it is detected that the build is occurring inside a Build
Server then the [variables](/docs/reference/variables) will be written to the
Build Server log in a format that the current Build Server can consume. See
[Build Server Support](/docs/reference/build-servers).

## Conditional control tasks

Properties `WriteVersionInfoToBuildLog`, `UpdateAssemblyInfo`,
`UseFullSemVerForNuGet`, `UpdateVersionProperties` and `GetVersion` are checked
before running these tasks.

You can disable `GitVersion.MsBuild.UpdateAssemblyInfo` by setting
`UpdateAssemblyInfo` to `false` in your MSBuild script, like
this:

```xml
<PropertyGroup>
  ...
  <UpdateAssemblyInfo>false</UpdateAssemblyInfo>
  ...
</PropertyGroup>
```

For SDK-style projects, `UpdateVersionProperties` controls setting the default
variables: `Version`, `VersionPrefix`, `VersionSuffix`, `PackageVersion`,
`InformationalVersion`, `AssemblyVersion` and `FileVersion`.

## Overriding Target Framework

If you want to override the target framework that GitVersion uses to determine the version, you can set the `GitVersionTargetFramework` property in your MSBuild script, like this:

```xml
<PropertyGroup>
  ...
  <GitVersionTargetFramework>net8.0</GitVersionTargetFramework>
  ...
</PropertyGroup>
```

### Namespace generation

You can configure GitVersion to generate the `GitVersionInformation` class in a namespace that matches the current assembly. By default this class is created in the global namespace. If `UseProjectNamespaceForGitVersionInformation` is set to true, the `GitVersionInfomation` class will instead be generated in a namespace matching the current project. If the property `<RootNamespace>` is set that value will be used, otherwise the name of the project file is used.

```xml
<PropertyGroup>
  ...
  <UseProjectNamespaceForGitVersionInformation>true</UseProjectNamespaceForGitVersionInformation>
  ...
</PropertyGroup>
```

## Extra properties

There are properties that correspond to certain
[command line arguments](/docs/usage/cli/arguments) for GetVersion task.
In particular, setting `GitVersion_NoFetchEnabled` to `true` disables `git fetch`
during version calculation, setting `GitVersion_NoNormalizeEnabled` to `true` disables
normalize step on a build server, setting `GitVersion_NoCacheEnabled` to `true`
makes GetVersion ignore cache. All the rest command line arguments can be passed via
`GitVersion_CommandLineArguments` variable.

## My Git repository requires authentication. What should I do?

Set the environment variables `GITVERSION_REMOTE_USERNAME` and
`GITVERSION_REMOTE_PASSWORD` before the build is initiated.
