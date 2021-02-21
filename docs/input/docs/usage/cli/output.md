---
Order: 30
Title: Output
Description: Details about the output types supported by the GitVersion CLI
---

By default GitVersion returns a json object to stdout containing all the
[variables](../more-info/variables) which GitVersion generates. This works
great if you want to get your build scripts to parse the json object then use
the variables, but there is a simpler way.

`GitVersion.exe /output buildserver` will change the mode of GitVersion to write
out the variables to whatever build server it is running in. You can then use
those variables in your build scripts or run different tools to create versioned
NuGet packages or whatever you would like to do. See [build
servers](/docs/reference/build-servers) for more information about this.
