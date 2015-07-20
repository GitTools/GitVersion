# Build Server Support
GitVersion has support for quite a few build servers out of the box. Currently we support:

 - TeamCity
 - AppVeyor
 - Continua Ci
 - MyGet

When GitVersion.exe is run with the `/output buildserver` flag instead of outputting Json it will export variables to the current build server.
For instance if you are running in TeamCity after you run `GitVersion /output buildserver` you will have the `%system.GitVersion.SemVer%` available for you to use

When running in MSBuild either from the MSBuild Task or by using the `/proj myproject.sln` GitVersion will make the MSBuild variables available in the format `$(GitVersion_SemVer)`.

## Setup guides
 - [AppVeyor](more-info/build-server-setup/appveyor.md)
 - [TeamCity](more-info/build-server-setup/teamCity.md)
 - [AppVeyor](more-info/build-servers/appveyor.md)
 - [TeamCity](more-info/build-servers/teamcity.md)
 - [MyGet](more-info/build-servers/myget.md)
 - [Bamboo](more-info/build-servers/bamboo.md)
 - [Jenkins](more-info/build-servers/jenkins.md)
 - [Continua CI](more-info/build-servers/continua.md))
 - [Team Build (TFS)](more-info/build-servers/teambuild.md)
 - [TFS Build vNext](more-info/build-servers/tfs-build-vnext.md)
