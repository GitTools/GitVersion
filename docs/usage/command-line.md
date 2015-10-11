# Command Line

If you want a command line version installed on your machine then you can use [Chocolatey](http://chocolatey.org) to install GitVersion

Available on [Chocolatey](http://chocolatey.org) under [GitVersion.Portable](http://chocolatey.org/packages/GitVersion.Portable)

 > choco install GitVersion.Portable

Switches are available with `GitVersion /?`

## Output

By default GitVersion returns a json object to stdout containing all the [variables](more-info/variables.md) which GitVersion generates. This works great if you want to get your build scripts to parse the json object then use the variables, but there is a simpler way.

`GitVersion.exe /output buildserver` will change the mode of GitVersion to write out the variables to whatever build server it is running in. You can then use those variables in your build scripts or run different tools to create versioned NuGet packages or whatever you would like to do. See [build servers](build-server-support.md) for more information about this.