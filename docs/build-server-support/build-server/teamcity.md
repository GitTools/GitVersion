# TeamCity Setup
## Basic Usage
In [TeamCity](https://www.jetbrains.com/teamcity/) you can create a build step as follows:

* **Runner type:** Command Line
* **Run:** Executable with parameters
* **Command executable:**  `GitVersion.exe`
* **Command parameters:** `/output buildserver /updateassemblyinfo true`

Then in your build parameters simply [add a placeholder](#nuget-in-teamcity) of the GitVersion variables you would like to use.

GitVersion writes system parameters into TeamCity, so they will automatically be passed to your build scripts to use.

## GitVersion meta runner for TeamCity
TeamCity has support for meta-runners which allow custom tasks. There is a GitVersion meta-runner available which makes it easy to use GitVersion.

 - [Project Link](https://github.com/JetBrains/meta-runner-power-pack/tree/master/gitversion)

## Running inside TeamCity
When running in TeamCIty you have two options, run using **agent checkout** or use dynamic repositories.

### Agent checkout
For GitVersion to pick up pull requests properly you need to promote the `%teamcity.build.vcs.branch.{configurationid}%` variable to an environment variable called `Git_Branch`

Just go to your build configuration, Parameters, click Add, Name should be `env.Git_Branch`, value should be `%teamcity.build.vcs.branch.{vcsid}%` where vcsid is your VCS root id. You should get auto completion for this.

### Dynamic repositories
To use server side checkout, you must use the dynamic repositories feature of GitVersion. Server side checkout sends just the files to the agent and not the actual .git folder. Dynamic repositories will clone your repo into a temp folder and use it to calculate version information.

See [dynamic repositories](../../more-info/dynamic-repositories.md) for more info.

### Output
* We update the TC build number to the GitVersion number automatically
* We output the individual values of the GitVersion version variables as build parameters with format `GitVersion.*` (Eg: `GitVersion.Major`) if you need access to them in your build script. Being system variables they will be passed as msbuild/environmental variables to other build steps

### NuGet in TeamCity
* Add dummy [parameter](http://confluence.jetbrains.com/display/TCD8/Configuring+Build+Parameters) to
the project called `GitVersion.NuGetVersion`. If many of your projects uses git-flow and SemVer you
can add the parameter to the "root-project" (TeamCity 8.x+). You need a dummy param because GitVersion creates the variables at runtime, and you cannot reference a paramter which is not available statically. GitVersion will overwrite the dummy value
* Then setup you nuget pack build set the "version" to `%GitVersion.NuGetVersion%`
* If you do your pack in a build script then you can just use environmental variables because teamcity will pass them through automatically.

### When TeamCity -> GitHub can't use https
GitVersion requires the presence of master branch in order to determine the version number.  If TeamCity uses https to clone git repos then GitVersion will pull down master branch for you during the build.

If however your TeamCity uses SSH to clone git repos and https is unavailable then GitVersion will error with a message like

> [GitVersionTask.UpdateAssemblyInfo] Error occurred: GitVersion.MissingBranchException: Could not fetch from 'git@github.dev.xero.com:Xero/Bus.git' since LibGit2 does not support the transport. You have most likely cloned using SSH. If there is a remote branch named 'master' then fetch it manually, otherwise please create a local branch named 'master'. ---> LibGit2Sharp.LibGit2SharpException: An error was raised by libgit2. Category = Net (Error).
This transport isn't implemented. Sorry

## Guides
 - [Continuous Delivery Setup in TeamCity](http://jake.ginnivan.net/blog/2014/07/09/my-typical-teamcity-build-setup)
