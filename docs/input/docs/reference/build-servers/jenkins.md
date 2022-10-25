---
Order: 70
Title: Jenkins
Description: Details on the Jenkins support in GitVersion
RedirectFrom: docs/build-server-support/build-server/jenkins
---

## SCM Settings

When setting up a Jenkins project for GitVersion, it is necessary to add a few _Behaviors_ to the SCM settings to ensure that GitVersion has enough information:

*   Advanced clone behaviors
    *   Enable `Fetch tags`
    *   Enable `Honor refspec on intial clone`
*   Check out to matching local branch
*   Prune stale remote-tracking branches
*   Specify ref specs
    *   Ref Spec: `+refs/heads/*:refs/remotes/@{remote}/*`

## Usage

Integrating GitVersion into your Jenkins build varies based on the project type: Freestyle vs. Pipeline.

### Freestyle Projects

Injecting environment variables is not supported in Jenkins natively, but
Jenkins plugins exist that provide this functionality. Of these plugins
[EnvInject][env-inject] appears to be the most popular with over 20k downloads per month.

To inject the GitVersion variables as environment variables for a build job
using [EnvInject][env-inject], do the following:

1.  Add an **Execute Windows batch command** build step with _Command_:
    `gitversion /output buildserver`
2.  Add an **Inject environment variables** build step and use value
    'gitversion.properties' for the _Properties File Path_ parameter

This assumes GitVersion.exe is available on the command line.

You can verify correct injection of environment variables by adding another
"Execute Windows batch command" build step with the following _Command_:

```shell
@echo Retrieving some GitVersion environment variables:
@echo %GitVersion_SemVer%
@echo %GitVersion_BranchName%
@echo %GitVersion_AssemblySemVer%
@echo %GitVersion_MajorMinorPatch%
@echo %GitVersion_Sha%
```

### Pipeline Projects

For pipeline projects, GitVersion variables can be accessed by reading the `gitversion.properties` file using the [Pipeline Utility Steps][pipeline-utility-steps] plugin. Variables from a property file are not automatically merged with the environment variables, but they can be accessed within a script block.

In a pipeline stage:

1.  Run GitVersion with the flag for _buildserver_ output (this only works when run from Jenkins, specifically when the `JENKINS_URL` environment variable is defined):

```groovy
sh 'gitversion /output buildserver'`
```

2.  Add a script block to read the properties file, assign environment variables as needed:

```groovy
script {
    def props = readProperties file: 'gitversion.properties'

    env.GitVersion_SemVer = props.GitVersion_SemVer
    env.GitVersion_BranchName = props.GitVersion_BranchName
    env.GitVersion_AssemblySemVer = props.GitVersion_AssemblySemVer
    env.GitVersion_MajorMinorPatch = props.GitVersion_MajorMinorPatch
    env.GitVersion_Sha = props.GitVersion_Sha
}
```

[env-inject]: https://wiki.jenkins-ci.org/display/JENKINS/EnvInject+Plugin
[pipeline-utility-steps]: https://plugins.jenkins.io/pipeline-utility-steps
