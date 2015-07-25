# Jenkins Setup

Injecting environment variables is not supported in Jenkins natively, but Jenkins plugins exist that provide this functionality. Of these plugins [EnvInject] appears to be the most popular with over 20k downloads per month.

To inject the GitVersion variables as environment variables for a build job using  [EnvInject], do the following:

 1. Add a **Execute Windows batch command** build step with command:
    `gitversion /output props > gitversion.properties`
 1. Add a **Inject environment variables** build step and use value 'gitversion.properties" for the "Properties File Path" parameter 

You can verify correct inject of environment variables by adding another "Execute Windows batch command" build step with the following command:

```
@echo Retrieving some GitVersion environment variables:
@echo %gitversion.SemVer%
@echo %gitversion.BranchName%
@echo %gitversion.AssemblySemVer%
@echo %gitversion.MajorMinorPatch%
@echo %gitversion.Sha%
```

  [EnvInject]: https://wiki.jenkins-ci.org/display/JENKINS/EnvInject+Plugin