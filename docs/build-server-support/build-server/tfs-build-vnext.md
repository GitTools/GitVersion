# Team Foundation Build (vNext) Setup
## Basic Usage
In Team Foundation Build (the web based build system) you can call GitVersion either using the Command Line build step or install an extension / custom build step. The custom build step requires a one-time setup to import the GitVersion task into your TFS or VSTS instance.

## Executing GitVersion
### Using GitVersion with the MSBuild Task NuGet Package
1. Add the [GitVersionTask](https://www.nuget.org/packages/GitVersionTask/) NuGet package to your projects.

See [MSBuild Task](/usage/msbuild-task) for further instructions how to use the MS Build Task.

### Using GitVersion with the Command Line build step
1. Make sure to have GitVersion.exe under version control. There exists also a [Chocolatey package](https://chocolatey.org/packages/GitVersion.Portable) for installing GitVersion.exe on build agents.
2. Add a Command Line build step to your build definition. You'll probably want to drag the task to be at or near the top to ensure it executes before your other build steps.
3. Set the Tool parameter to `<pathToGitVersion>\GitVersion.exe`.
4. Set the Arguments parameter to `/output buildserver /nofetch`.
5. If you want the GitVersionTask to update AssemblyInfo files add `updateAssemblyInfo true` to the Arguments parameter. 
6. If you want to update the build number you need to send a [logging command](https://github.com/Microsoft/vso-agent-tasks/blob/master/docs/authoring/commands.md) to TFS.

### Using the custom GitVersion build step
#### Installing
##### Installing the extension
For Visual Studio Team Service or TFS 2015 Update 2 or higher it is recommonded to install the GitVersion extension:
1. Install the [GitVersion Extension](https://marketplace.visualstudio.com/items?itemName=gittools.gitversion).

##### Manually installing/updating the custom build step
If you run TFS 2015 RTM or Update 1 or don't want to install the GitVersion extension you can install the build task manually:
1. Install the `tfx` command line tool as shown [here](https://github.com/Microsoft/tfs-cli/blob/master/README.md#install).
2. For TFS 2015 On-Prem configure Basic Authentication in TFS as shown [here](https://github.com/Microsoft/tfs-cli/blob/master/docs/configureBasicAuth.md).
3. Download the GitVersion TFS build task from the latest release on the [GitVersion releases page](https://github.com/GitTools/GitVersion/releases) and unzip.
4. Run `tfx login` as shown [here](https://github.com/Microsoft/tfs-cli/blob/master/README.md#login).
5. From the directory outside of where you unzipped the task, run `tfx build tasks upload --task-path .\GitVersionTfsTask --overwrite` where GitVersionTfsTask is the directory containing the files.
6. It should successfully install.

#### Using the GitVersion custom build step
From a TFS build definition, select "Add a Step" and then in the Build category, choose GitVersion and click Add. You'll probably want to drag the task to be at or near the top to ensure it executes before your other build steps.

If you want the GitVersionTask to update AssemblyInfo files, check the box in the task configuration. For advanced usage, you can pass additional options to the GitVersion exe in the Additional arguments section.

The VSO build step can update your build number with GitVersion variables. See below for details.


## Running inside TFS
### Using the GitVersion Variables
GitVersion passes variables in the form of `GitVersion.*` (Eg: `GitVersion.Major`) to TFS Build and also writes `GITVERSION_*` (Eg: `GITVERSION_MAJOR`) environment variables that are available for any subsequent build step.

To use these variables you can just refer to them using the standard variable syntax. For instance `$(GitVersion_NuGetVersion)` in your nuget pack task to set the version number. Since update 1 there are no known limitations.

See [Variables](/more-info/variables/) for an overview of available variables.


#### Using GitVersion variables in build name
To use GitVersion's variables in the build name, just add them in the form `$(GITVERSION_FullSemVer)` into the Build definition's build number string. Then just ensure GitVersion is called with
`/output buildserver` and it will replace those variables with the calculated version.
The TFS GitVersion Build Step (above) handles this too, so if you're already using that, there's nothing extra to configure.

If GitVersion does not find any substitutions it will just default to using `FullSemVer`

**IMPORTANT:** If you currently use `$(rev:.r)` in your build number, that won't work correctly if you 
use GitVersion variables as well due to the delayed expansion of the GitVersion vars. Instead,
You might be able to use `$(GitVersion_BuildMetaData)` to achieve a similar result.
See [Variables](/more-info/variables/) for more info on the variables.

#### Known limitations
* If you are using on premises TFS, make sure you are using at least **TFS 2015 Update 1**, otherwise a few things will not work.
* Installing the extension on an on premise TFS requires at least TFS 2015 Update 2.
* You need to make sure that all tags are fetched for the Git repository, otherwise you may end with wrong versions (e.g. `FullSemVer` like `1.2.0+5` instead of `1.2.0` for tagged releases) 
Just checking the `Clean Repository` check box in the build definition settings might not be enough since this will run a `git clean -fdx/reset --hard` without fetching all tags later. 
You can force deletion of the whole folder and a re-clone containing all tags by settings the variable `Build.Clean` to `all`.
This will take more time during build but makes sure that all tags are fetched.
In the future it is planned to allow using `git.exe` instead of current `libgit2sharp` for syncing the repos which might allow other possibilities to solve this issue. 
For details see this [GitHub issue](https://github.com/Microsoft/vso-agent-tasks/issues/1218).
* If running a build for a certain commit (through passing the commit SHA while queueing the build) all tags from the repository will be fetched, even the ones newer than the commit.
This can lead to different version numbers while re-running historical builds.  
