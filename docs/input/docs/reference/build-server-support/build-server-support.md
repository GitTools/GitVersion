---
Order: 10
Title: Introduction
Description: Information about what GitVersion can do on Build Servers
---

GitVersion has support for quite a few build servers out of the box. Currently
we support:

- [AppVeyor](build-server/appveyor)
- [Azure DevOps](build-server/azure-devops)
- [Bamboo](build-server/bamboo)
- [Continua CI](build-server/continua)
- [GitLab CI](build-server/gitlab)
- [Jenkins](build-server/jenkins)
- [MyGet](build-server/myget)
- [Octopus Deploy](build-server/octopus-deploy)
- [TeamCity](build-server/teamcity)

When GitVersion.exe is run with the `/output buildserver` flag instead of
outputting Json it will export variables to the current build server.  For
instance if you are running in TeamCity after you run
`GitVersion /output buildserver` you will have the `%system.GitVersion.SemVer%`
available for you to use.

When running in MSBuild either from the [MSBuild Task](../usage/msbuild-task) or
by using the `/proj myproject.sln` parameter, GitVersion will make the MSBuild
variables available in the format `$(GitVersion_SemVer)`.

Standard GitVersion.exe normalize the branches if there is a build server
detected. This behavior can be disabled with the `/nonormalize` option.
