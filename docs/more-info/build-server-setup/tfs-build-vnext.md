# Visual Studio Online (Build vNext) Setup
## Basic Usage
In [Visual Studio Online](https://www.visualstudio.com/) build vNext (the web based build system) you can add a build step as follows:

* **Build Step:** Command Line
	* Might need to fully qualify the path.
	* Tip: Use a script and/or another command step to call `NuGet Install GitVersion.CommandLine` first so you don't have to check in the exe.
* **Tool:**  `GitVersion.exe`
* **Arguments:** `/output buildserver /updateassemblyinfo true`

Then in your build parameters simply [add a placeholder](#nuget-in-teamcity) of the GitVersion variables you would like to use.

GitVersion writes build parameters into VSO, so they will automatically be passed to your build scripts to use.

## GitVersion Build Step for VSO
Visual Studio Online has support for custom build steps. This is planned but TBD. For now, the command line does work.

 
## Running inside TeamCity
* We output the individual values of the GitVersion version as the build parameter: `GitVersion.*` (Eg: `GitVersion.Major`) if you need access to them in your build script

### NuGet in VSO
* Add dummy parameter to the project called `GitVersion.NuGetVersion`.
* Then setup you nuget pack build set the "version" to `%GitVersion.NuGetVersion%`


