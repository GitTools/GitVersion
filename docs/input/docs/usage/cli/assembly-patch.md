---
Order: 40
Title: Assembly Patching
Description: |
    Read more about how GitVersion can patch the version number into your
    assemblies
---

`GitVersion.exe /updateassemblyinfo` will recursively search for all
`AssemblyInfo.cs` or `AssemblyInfo.vb` files in the git repo and update them.
It will update the following assembly attributes:

* `AssemblyVersion` will be set to the `AssemblySemVer` variable.
* `AssemblyFileVersion` will be set to the `MajorMinorPatch` variable with an
appended `.0`.
* `AssemblyInformationalVersion` will be set to the `InformationalVersion`
variable.

Note that contrary to when using the [MSBuild Task](msbuild-task) the
attributes must already exist in the `AssemblyInfo.cs` or `AssemblyInfo.vb`
files prior to calling GitVersion.

By adding `/updateassemblyinfo <filenames>` the name of AssemblyInfo file to
update can be set.  This switch can accept multiple files with the path to the
file specified relative to the working directory.

GitVersion can generate an assembly info source file for you if it does not
already exist.  Use the `/ensureassemblyinfo` switch alongside
`/updateassemblyinfo <filename>`, if the filename specified does not exist it
will be generated based on a known template that adds:

* `AssemblyVersion` will be set to the `AssemblySemVer` variable.
* `AssemblyFileVersion` will be set to the `MajorMinorPatch` variable with an
appended `.0`.
* `AssemblyInformationalVersion` will be set to the `InformationalVersion`
variable.

This can be done for *.cs, *.vb and *.fs files.

When requesting that GitVersion generate an assembly info file you are limited
to only specifying a single `<filename>` within the `/updateassemblyinfo`
switch, this is to prevent the creation of multiple assembly info files with the
same assembly version attributes.  If this occurs your build will fail.

## Example: When AssemblyInfo.cs does not exist

`GitVersion.exe /updateassemblyinfo AssemblyInfo.cs /ensureassemblyinfo`

A file is generated that contains version attributes (`AssemblyVersion`,
`AssemblyFileVersion`, `AssemblyInformationalVersion`)

## Example: When AssemblyInfo.cs already exists

`GitVersion.exe /updateassemblyinfo AssemblyInfo.cs /ensureassemblyinfo`

All known attributes (`AssemblyVersion`, `AssemblyFileVersion`,
`AssemblyInformationalVersion`) will be updated

## Example: When AssemblyInfo.cs and AssemblyVersionInfo.cs do not exist

`GitVersion.exe /updateassemblyinfo AssemblyInfo.cs AssemblyVersionInfo.cs /ensureassemblyinfo`

Will result in command line argument error

## Example: When AssemblyInfo.cs and AssemblyVersionInfo.cs already exist

`GitVersion.exe /updateassemblyinfo AssemblyInfo.cs AssemblyVersionInfo.cs`

Will iterate through each file and update known attributes (`AssemblyVersion`,
`AssemblyFileVersion`, `AssemblyInformationalVersion`).

## Example: How to override configuration option 'tag-prefix' to use prefix 'custom'

`GitVersion.exe /output json /overrideconfig tag-prefix=custom`

## Writing version metadata in WiX format

To support integration with WiX projects, use `GitVersion.exe /updatewixversionfile`.
All the [variables](../more-info/variables) are written to
`GitVersion_WixVersion.wxi` under the current working directory and can be
referenced in the WiX project files.

[docker]: https://hub.docker.com/r/gittools/gitversion
[choco]: http://chocolatey.org/packages/GitVersion.Portable
[brew]: https://formulae.brew.sh/formula-linux/gitversion
[tool]: https://www.nuget.org/packages/GitVersion.Tool/
[dotnet-tool]: https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools#install-a-global-tool
