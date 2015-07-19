# TeamCity Setup
## Basic Usage
In [TeamCity](https://www.jetbrains.com/teamcity/) you can create a build step as follows:

* **Runner type:** Command Line
* **Run:** Executable with parameters
* **Command executable:**  `GitVersion.exe`
* **Command parameters:** `/output buildserver /updateassemblyinfo true`

Then in your build parameters simply [add a placeholder](https://github.com/Particular/GitVersion/wiki/Variables#recommended-teamcity-setup) of the GitVersion variables you would like to use.

GitVersion writes system parameters into TeamCity, so they will automatically be passed to your build scripts to use.

## Running inside TeamCity
* Make sure to use **agent checkouts** (required, server checkouts do not copy the needed `.git` directory)
  - If you want to use *checkout on server*, see [dynamic repositories](Dynamic-Repositories.md)
* For the moment you need to promote the `%teamcity.build.vcs.branch.{configurationid}%` build parameter to an environment variable with the same name for pull requests to be handled correctly
* We update the TC build number to the GitVersion number automatically
* We output the individual values of the GitVersion version as the build parameter: `GitVersion.*` (Eg: `GitVersion.Major`) if you need access to them in your build script

### NuGet in TeamCity
* Add dummy [parameter](http://confluence.jetbrains.com/display/TCD8/Configuring+Build+Parameters) to
the project called `GitVersion.NuGetVersion`. If many of your projects uses git-flow and SemVer you
can add the parameter to the "root-project" (TeamCity 8.x+)
* Then setup you nuget pack build set the "version" to `%GitVersion.NuGetVersion%`

### When TeamCity -> GitHub can't use https
GitVersion requires the presence of master branch in order to determine the version number.  If TeamCity uses https to clone git repos then GitVersion will pull down master branch for you during the build.

If however your TeamCity uses SSH to clone git repos and https is unavailable then GitVersion will error with a message like

> [GitVersionTask.UpdateAssemblyInfo] Error occurred: GitVersion.MissingBranchException: Could not fetch from 'git@github.dev.xero.com:Xero/Bus.git' since LibGit2 does not support the transport. You have most likely cloned using SSH. If there is a remote branch named 'master' then fetch it manually, otherwise please create a local branch named 'master'. ---> LibGit2Sharp.LibGit2SharpException: An error was raised by libgit2. Category = Net (Error).
This transport isn't implemented. Sorry

You need to create a TeamCity build step before your compile step which manually creates a local master branch which tracks remote master.  Like so (in powershell):

```Powershell
$branchBeingBuilt = . git symbolic-ref --short -q HEAD  
. git pull 2>&1 | write-host
foreach ($remoteBranch in . git branch -r) {
  . git checkout $remoteBranch.Trim().Replace("origin/", "") 2>&1 | write-host
  . git pull 2>&1 | write-host  
}  
. git checkout $branchBeingBuilt 2>&1 | write-host  
exit 0
```

you should get build output like

```
[Step 1/1]: Ensure all branches are available for GitVersion (Powershell) (5s)
[Step 1/1] From file:///C:/BuildAgent2/system/git/git-12345678
[Step 1/1]  * [new branch]      master     -> origin/master
[Step 1/1] Switched to a new branch 'master'
[Step 1/1] Branch master set up to track remote branch master from origin.
[Step 1/1] Switched to branch 'develop'
```

## Guides
 - [Continuous Delivery Setup in TeamCity](http://jake.ginnivan.net/blog/2014/07/09/my-typical-teamcity-build-setup)
