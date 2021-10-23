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

It currently works with desktop `MSBuild`. Support for CoreCLR with `dotnet build`
is coming soon.

> **Note**\
> The nuget package was "_[GitVersionTask](https://www.nuget.org/packages/GitVersionTask/)_" up until version 5.5.1.\
> From version 5.6.0 it has been called "_[GitVersion.MsBuild](https://www.nuget.org/packages/GitVersion.MsBuild/)_"

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
<PackageReference Include="GitVersion.MsBuild" Version="5.6.10*">
  <PrivateAssets>All</PrivateAssets>
</PackageReference>
```

### Remove AssemblyInfo attributes

The next thing you need to do is to remove the `Assembly*Version` attributes from
your `Properties\AssemblyInfo.cs` files. This puts GitVersion.MsBuild in charge of
versioning your assemblies.

### WPF specific concerns

One further step needs to be taken for SDK-style WPF projects.

Building projects with .NET Core SDK with a version lower than v5.0.200
requires turning off automatic generation of the different versioning attributes.
GitVersion usually controls these properties but cannot during WPF specific
targets that generate a temporary project.

```xml
<PropertyGroup>
  <!-- Wpf workaround: GitVersion and .NET SDK < v5.0.200 -->
  <GenerateAssemblyFileVersionAttribute>false</GenerateAssemblyFileVersionAttribute>
  <GenerateAssemblyInformationalVersionAttribute>false</GenerateAssemblyInformationalVersionAttribute>
  <GenerateAssemblyVersionAttribute>false</GenerateAssemblyVersionAttribute>
</PropertyGroup>
```

For .NET Core SDK v5.0.200 to v6.0.0-preview.1, a opt-in flag was introduced to
allow package references to be imported to the temporary project.
You can now remove the previous versioning attributes and replace them with
a single property.

```xml
<PropertyGroup>
  <!-- WPF workaround: GitVersion and .NET SDK between v5.0.200 and v6.0.0-preview.2  -->
  <IncludePackageReferencesDuringMarkupCompilation>true</IncludePackageReferencesDuringMarkupCompilation>
</PropertyGroup>
```

You can remove all workarounds if you are building with .NET Core SDK
v6.0.0-preview.2 or later as the flag is now opt-out.

### Done!

The setup process is now complete and GitVersion.MsBuild should be working its magic,
versioning your assemblies like a champ. However, more can be done to further
customize the build process. Keep reading to find out how the version variables
are set and how you can use them in MSBuild tasks.

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

*   `AssemblyVersion` will be set to the `AssemblySemVer` variable.
*   `AssemblyFileVersion` will be set to the `MajorMinorPatch` variable with `.0`
    appended to it.
*   `AssemblyInformationalVersion` will be set to the `InformationalVersion` variable.

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

## My Git repository requires authentication. What should I do?

Set the environment variables `GITVERSION_REMOTE_USERNAME` and
`GITVERSION_REMOTE_PASSWORD` before the build is initiated.
