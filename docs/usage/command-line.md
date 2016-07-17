# Command Line

If you want a command line version installed on your machine then you can use [Chocolatey](http://chocolatey.org) to install GitVersion

Available on [Chocolatey](http://chocolatey.org) under [GitVersion.Portable](http://chocolatey.org/packages/GitVersion.Portable)

 > choco install GitVersion.Portable

Switches are available with `GitVersion /?`

## Output

By default GitVersion returns a json object to stdout containing all the [variables](../more-info/variables.md) which GitVersion generates. This works great if you want to get your build scripts to parse the json object then use the variables, but there is a simpler way.

`GitVersion.exe /output buildserver` will change the mode of GitVersion to write out the variables to whatever build server it is running in. You can then use those variables in your build scripts or run different tools to create versioned NuGet packages or whatever you would like to do. See [build servers](../build-server-support/build-server-support.md) for more information about this.

## Inject version metadata into the assembly
`GitVersion.exe /updateassemblyinfo` will recursively search for all `AssemblyInfo.cs` or `AssemblyInfo.vb` files in the git repo and update them. 
It will update the following assembly attributes:

* `AssemblyVersion` will be set to the `AssemblySemVer` variable.
* `AssemblyFileVersion` will be set to the `MajorMinorPatch` variable with a appended `.0`.
* `AssemblyInformationalVersion` will be set to the `InformationalVersion` variable.

Note that contrary to when using the [MSBuild Task](msbuild-task.md) the attributes must already exist in the `AssemblyInfo.cs` or `AssemblyInfo.vb` files prior to calling GitVersion.

By adding `/updateassemblyinfo <filenames>` the name of AssemblyInfo file to update can be set.  This switch can accept multiple files with the path to the file specified relative to the working directory. 

GitVersion can generate an assembly info source file for you if it does not already exist.  Use the `/ensureassemblyinfo` switch alongside `/updateassemblyinfo <filename>`, if the filename specified does not exist it will be generated based on a known template that adds:

* `AssemblyVersion` will be set to the `AssemblySemVer` variable.
* `AssemblyFileVersion` will be set to the `MajorMinorPatch` variable with a appended `.0`.
* `AssemblyInformationalVersion` will be set to the `InformationalVersion` variable.

This can be done for *.cs, *.vb and *.fs files. 

When requesting that GitVersion generate an assembly info file you are limited to only specifying a single `<filename>` within the `/updateassemblyinfo` switch, this is to prevent the creation of mulitple assembly info files with the same assembly version attributes.  If this occurs your build will fail.

### Example: When AssemblyInfo.cs does not exist
`GitVersion.exe /updateassemblyinfo AssemblyInfo.cs /ensureassemblyinfo`

A file is generated that contains version attributes (`AssemblyVersion`, `AssemblyFileVersion`, `AssemblyInformationalVersion`)

### Example: When AssemblyInfo.cs already exists
`GitVersion.exe /updateassemblyinfo AssemblyInfo.cs /ensureassemblyinfo`

All known attributes (`AssemblyVersion`, `AssemblyFileVersion`, `AssemblyInformationalVersion`) will be updated

### Example: When AssemblyInfo.cs and AssemblyVersionInfo.cs do not exist
`GitVersion.exe /updateassemblyinfo AssemblyInfo.cs AssemblyVersionInfo.cs /ensureassemblyinfo`

Will result in command line argument error

### Example: When AssemblyInfo.cs and AssemblyVersionInfo.cs already exist
`GitVersion.exe /updateassemblyinfo AssemblyInfo.cs AssemblyVersionInfo.cs`

Will iterate through each file and update known attributes (`AssemblyVersion`, `AssemblyFileVersion`, `AssemblyInformationalVersion`).

## Override config
`/overrideconfig [key=value]` will override appropriate key from 'GitVersion.yml'. 

At the moment only `tag-prefix` option is supported. Read more about [Configuration](/configuration/).

It will not change config file 'GitVersion.yml'.

### Example: How to override configuration option 'tag-prefix' to use prefix 'custom'
`GitVersion.exe /output json /overrideconfig tag-prefix=custom`