# Build Server Support
GitVersion has support for quite a few build servers out of the box. Currently we support:

 - TeamCity
 - AppVeyor
 - Continua Ci
 - MyGet

When GitVersion.exe is run with the `/output buildserver` flag instead of outputting Json it will export variables to the current build server.
For instance if you are running in TeamCity after you run `GitVersion /output buildserver` you will have the `%system.GitVersion.SemVer%` available for you to use

When running in MSBuild either from the MSBuild Task or by using the `/proj myproject.sln` GitVersion will make the MSBuild variables available in the format `$(GitVersion_SemVer)`.

## Setup guides
 - [AppVeyor](buildServerSetup/AppVeyor.md)
 - [TeamCity](buildServerSetup/teamCity.md)

## Other plugins/helpers
### GitVersion meta runner for TeamCity
TeamCity has support for meta-runners which allow custom tasks. There is a GitVersion meta-runner available which makes it easy to use GitVersion.

 - [Project Link](https://github.com/JetBrains/meta-runner-power-pack/tree/master/gitversion)

### GitVersion for Bamboo
If you use Bamboo then you can install *GitVersion for Bamboo* which gives you a GitVersion task in Bamboo.

 - [Blog Post](http://carolynvanslyck.com/blog/2015/03/gitversion-for-bamboo)
 - [Project link](http://carolynvanslyck.com/projects/gitversion)
 - [Download](https://marketplace.atlassian.com/plugins/com.carolynvs.gitversion)
 - [Source](http://carolynvanslyck.com/projects/gitversion)
 - [Issues](http://jira.carolynvanslyck.com/browse/GITVER)
