# Team Foundation Build (vNext) Setup
## Basic Usage
In Team Foundation Build (the web based build system) you can call GitVersion either using the Command Line build step or install a custom build step. The custom build step requires a one-time setup to import the GitVersion task into your TFS or VSO instance.

## Executing GitVersion
### Using GitVersion with the MSBuild Task NuGet Package
1. Add the [GitVersionTask](https://www.nuget.org/packages/GitVersionTask/) NuGet package to your projects.

See [MSBuild Task](/usage/#msbuild-task) for further instructions how to use the MS Build Task.

### Using GitVersion with the Command Line build step
1. Make sure to have GitVersion.exe under version control. There exists also a [Chocolatey package](https://chocolatey.org/packages/GitVersion.Portable) for installing GitVersion.exe on build agents.
2. Add a Command Line build step to your build definition. You'll probably want to drag the task to be at or near the top to ensure it executes before your other build steps.
3. Set the Tool parameter to `<pathToGitVersion>\GitVersion.exe`.
4. Set the Arguments parameter to `/output buildserver /nofetch`.
5. If you want the GitVersionTask to update AssemblyInfo files add `updateAssemblyInfo true` to the Arguments parameter. 
6. If you want to update the build number you need to send a [logging command](https://github.com/Microsoft/vso-agent-tasks/blob/master/docs/authoring/commands.md) to TFS.

### Using the custom GitVersion build step
#### Installing/updating the custom build step
1. Install the `tfx` command line tool as shown [here](https://github.com/Microsoft/tfs-cli/blob/master/README.md#install).
2. For TFS 2015 On-Prem configure Basic Authentication in TFS as shown [here](https://github.com/Microsoft/tfs-cli/blob/master/docs/configureBasicAuth.md).
3. Download the GitVersion TFS build task from the latest release on the [GitVersion releases page](https://github.com/GitTools/GitVersion/releases) and unzip.
4. Run `tfx login` as shown [here](https://github.com/Microsoft/tfs-cli/blob/master/README.md#login).
5. From the directory outside of where you unzipped the task, run `tfx build tasks upload .\GitVersionTfsTask --overwrite` where GitVersionTfsTask is the directory containing the files.
6. It should successfully install.

#### Using the GitVersion custom build step
From a TFS build definition, select "Add a Step" and then in the Build category, choose GitVersion and click Add. You'll probably want to drag the task to be at or near the top to ensure it executes before your other build steps.

If you want the GitVersionTask to update AssemblyInfo files, check the box in the task configuration. For advanced usage, you can pass additional options to the GitVersion exe in the Additional arguments section.

The VSO build step updates the build number automatically to the GitVersion number.

## Running inside TFS
### Using the GitVersion Variables
GitVersion passes variables in the form of `GitVersion.*` (Eg: `GitVersion.Major`) to TFS Build and also writes `GITVERSION_*` (Eg: `GITVERSION_MAJOR`) environment variables that are available for any subsequent build step. 
See [Variables](/more-info/variables/) for an overview of available variables.


#### Using GitVersion variables in build name
To use GitVersion's variables in the build name, just add them in the form `$(GITVERSION_FullSemVer)` into the Build definition's build number string. Then just ensure GitVersion is called with
`/output buildserver` and it will replace those variables with the calculated version.


#### Known limitations
* Due to [current limitations in TFS2015 On-Prem](https://github.com/Microsoft/vso-agent-tasks/issues/380) it's currently not possible to automatically set the build version in TFS2015 On-Prem. Instead a warning similar to `##[warning]Unable to process logging event:##vso[build.updatebuildnumber 1.0.0-unstable.1` is logged.
* Due to a know limitation in TFS 2015 On-Prem it's currently not possible to use variables added during build in inputs of subsequent build tasks, since the variables are processed at the beginning of the build. 
As a workaround environment variables can be used in custom scripts.

## Create a NuGet package in TFS
If you use a Command Line task to build your NuPkg, use `%GITVERSION_NUGETVERSION%` as the version parameter: `nuget.exe pack path\to\my.nuspec -version %GITVERSION_NUGETVERSION%`