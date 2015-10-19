# MSBuild Task

Available on [Nuget](https://www.nuget.org) under [GitVersionTask](https://www.nuget.org/packages/GitVersionTask/)

    Install-Package GitVersionTask

The MSBuild task will wire GitVersion into the MSBuild pipeline of a project and perform several actions.

## Inject version metadata into the assembly 

Task Name: `GitVersionTask.UpdateAssemblyInfo`

### AssemblyInfo Attributes

At build time a temporary AssemblyInfo.cs will be created that contains the appropriate SemVer information. This will will be included in the build pipeline.

Ensure you remove the `Assembly*Version` attributes from your `Properties\AssemblyInfo.cs` file so as to not get duplicate attribute warnings. Sample default:

    [assembly: AssemblyVersion("1.0.0.0")]
    [assembly: AssemblyFileVersion("1.0.0.0")]
    [assembly: AssemblyInformationalVersion("1.1.0+Branch.master.Sha.722aad3217bd49a6576b6f82f60884e612f9ba58")]

Now when you build:

* `AssemblyVersion` will be set to the `AssemblySemVer` variable.
* `AssemblyFileVersion` will be set to the `MajorMinorPatch` variable with a appended `.0`.
* `AssemblyInformationalVersion` will be set to the `InformationalVersion` variable.

### Other injected Variables

All other [variables](/more-info/variables.md) will be injected into a internal static class.

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

### Accessing injected Variables

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

## Populate some MSBuild properties with version metadata

Task Name: `GitVersionTask.GetVersion`

At build time all the derived [variables](/more-info/variables.md) will be written to MSBuild properties so the information can be used by other tooling in the build pipeline.

The class for `GitVersionTask.GetVersion` has a property for each variable. However at MSBuild time these properties a mapped to MSBuild properties that are prefixed with `GitVersion_`. This prevents conflicts with other properties in the pipeline.

### Accessing variable in MSBuild

After `GitVersionTask.GetVersion` has executed the properties can be used in the standard way. For example:

    <Message Text="GitVersion_InformationalVersion: $(GitVersion_InformationalVersion)"/> 

## Communicate variables to current Build Server

Task Name: `GitVersionTask.WriteVersionInfoToBuildLog`

If, at build time, it is detected that the build is occurring inside a Build Server server then the [variables](/more-info/variables.md) will be written to the build log in a format that the current Build Server can consume. See [Build Server Support](/build-server-support/build-server-support.md).

## My Git repository requires authentication. What do I do?

Set the environmental variables `GITVERSION_REMOTE_USERNAME` and `GITVERSION_REMOTE_PASSWORD` before the build is initiated.