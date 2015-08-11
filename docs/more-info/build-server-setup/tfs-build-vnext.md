# Visual Studio Online (Build vNext) Setup
## Basic Usage
In [Visual Studio Online](https://www.visualstudio.com/) build vNext (the web based build system) you can add GitVersion as a Build Step. This requires a one-time setup to import the GitVersion task into your VSO instance.

## Installing/updating the VSO Build Step
1. Install the `tfx` command line tool as shown [here](https://github.com/Microsoft/tfs-cli/blob/master/docs/buildtasks.md)

2. Download the GitVersion VSO extension from here **TODO determine link** and unzip.
3. Run `tfx login` if you haven't yet, make sure to use `https://<server>.visualstudio.com/DefaultCollection` as the URL and provide a Personal Access Token
4. From the directory outside of where you unzipped the task, run `tfx upload .\GitVersionVsoStep` where GitVersionVsoStep is the directory containing the files.
5. It should successfully install

## Using the GitVersion VSO Build Step
From a VSO vNext Build Definition, select "Add a Step" and then in the Build category, choose GitVersion and Click Add. You'll probably want to drag the task to be at or near the top to ensure it executes before your other build steps.

If you want the GitVersionTask to update AssemblyInfo files, check the box in the task configuration. For advanced usage, you can pass additional options to the GitVersion exe in the Additional arguments section.

## Using the GitVersion Variables
GitVersion writes build parameters into VSO, so they will automatically be passed to your build scripts to use. It also writes GITVERSION_* environment variables that are available for any subsequent build step.


 
## Running inside Visual Studio Online
* We output the individual values of the GitVersion version as the build parameter: `GitVersion.*` (Eg: `GitVersion.Major`) if you need access to them in your build script

### NuGet in VSO
* If you use a command script to build your NuPkg, use `%GITVERSION_NUGETVERSION%` as the version parameter: `nuget.exe pack path\to\my.nuspec -version %GITVERSION_NUGETVERSION%`



