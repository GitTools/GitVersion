# Build Server Support
GitVersion has support for quite a few build servers out of the box. Currently we support:

 - [AppVeyor](build-server/appveyor.md)
 - [Bamboo](build-server/bamboo.md)
 - [Continua CI](build-server/continua.md)
 - [GitLab CI](build-server/gitlab.md)
 - [Jenkins](build-server/jenkins.md)
 - [MyGet](build-server/myget.md)
 - [Octopus Deploy](build-server/octopus-deploy.md)
 - [TeamCity](build-server/teamcity.md)
 - [Team Build (TFS)](build-server/teambuild.md)
 - [TFS Build vNext](build-server/tfs-build-vnext.md)
 
When GitVersion.exe is run with the `/output buildserver` flag instead of outputting Json it will export variables to the current build server.
For instance if you are running in TeamCity after you run `GitVersion /output buildserver` you will have the `%system.GitVersion.SemVer%` available for you to use

When running in MSBuild either from the [MSBuild Task](/usage/msbuild-task) or by using the `/proj myproject.sln` parameter, GitVersion will make the MSBuild variables available in the format `$(GitVersion_SemVer)`.
