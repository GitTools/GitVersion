---
Order: 100
Title: TeamCity
Description: Details on the TeamCity support in GitVersion
RedirectFrom: docs/build-server-support/build-server/teamcity
---

## Basic Usage

In [TeamCity][teamcity] you can create a build step as follows:

*   **Runner type:** Command Line
*   **Run:** Executable with parameters
*   **Command executable:**  `GitVersion.exe`
*   **Command parameters:** `/output buildserver /updateassemblyinfo true`

Then in your build parameters simply [add a placeholder](#nuget-in-teamcity) of
the GitVersion variables you would like to use.

GitVersion writes system parameters into TeamCity, so they will automatically be
passed to your build scripts to use.

## GitVersion meta runner for TeamCity

TeamCity has support for meta-runners which allow custom tasks. There is a
[GitVersion meta-runner][meta-runner] available which makes it easy to use
GitVersion.

## Running inside TeamCity

When running in TeamCity you have two options, run using **agent checkout** or
use dynamic repositories.

### Agent checkout

For GitVersion to pick up pull requests properly you need to promote the
`%teamcity.build.vcs.branch.{vcsid}%` variable to an environment
variable called `Git_Branch`

Just go to your build configuration, Parameters, click Add, Name should be
`env.Git_Branch`, value should be `%teamcity.build.vcs.branch.{vcsid}%` where
`{vcsid}` is your VCS root id. You should get auto completion for this.

For GitVersion to work with any mode requiring other than the currently built
branch to calculate the version number, you need to set the configuration
parameter [`teamcity.git.fetchAllHeads = true` in TeamCity][general-settings],
because TeamCity by default fetches only the current branch for building.

To add this configuration parameter to your build configuration, go to
_Parameters_, click _Add_, _Name_ should be `teamcity.git.fetchAllHeads` and the
value should be `true`.

### Dynamic repositories

To use server side checkout, you must use the dynamic repositories feature of
GitVersion. Server side checkout sends just the files to the agent and not the
actual .git folder. Dynamic repositories will clone your repo into a temp folder
and use it to calculate version information.

See [dynamic repositories][dynamic-repo] for more info.

### Output

*   We update the TC build number to the GitVersion number automatically
*   We output the individual values of the GitVersion version variables as build
    parameters with format `GitVersion.*` (Eg: `GitVersion.Major`) if you need
    access to them in your build script. Being system variables they will be passed
    as msbuild/environmental variables to other build steps

### NuGet in TeamCity

*   Add a dummy [parameter][parameter] to the project called `GitVersion.NuGetVersion`. If
    many of your projects uses git-flow and SemVer you can add the parameter to
    the "root-project" (TeamCity 8.x+). You need a dummy param because
    GitVersion creates the variables at runtime, and you cannot reference a
    parameter which is not available statically. GitVersion will overwrite the
    dummy value.
*   Then setup you nuget pack build set the "version" to
    `%GitVersion.NuGetVersion%`.
*   If you do your pack in a build script then you can just use environmental
    variables because teamcity will pass them through automatically.

### When TeamCity -> GitHub can't use https

GitVersion requires the presence of main branch in order to determine the
version number.  If TeamCity uses https to clone git repos then GitVersion will
pull down main branch for you during the build.

If however your TeamCity uses SSH to clone git repos and https is unavailable
then GitVersion will error with a message like

```cs
[GitVersionTask.UpdateAssemblyInfo] Error occurred: GitVersion.MissingBranchException:
Could not fetch from 'git@github.dev.xero.com:Xero/Bus.git' since LibGit2 does
not support the transport. You have most likely cloned using SSH. If there is a
remote branch named 'main' then fetch it manually, otherwise please create a
local branch named 'main'. ---> LibGit2Sharp.LibGit2SharpException: An error
was raised by libgit2. Category = Net (Error). This transport isn't implemented.
Sorry
```

## Guides

*   [Continuous Delivery Setup in TeamCity][cd]

[cd]: https://jake.ginnivan.net/blog/2014/07/09/my-typical-teamcity-build-setup
[dynamic-repo]: /docs/learn/dynamic-repositories
[general-settings]: https://www.jetbrains.com/help/teamcity/git.html#General+Settings
[parameter]: https://confluence.jetbrains.com/display/TCD8/Configuring+Build+Parameters
[teamcity]: https://www.jetbrains.com/teamcity/
[meta-runner]: https://github.com/JetBrains/meta-runner-power-pack/tree/master/gitversion
