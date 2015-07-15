#MSBuild Task usage
The MSBuild task will wire GitVersion into the MSBuild pipeline of a project and automatically stamp that assembly with the appropriate SemVer information

Available on [Nuget](https://www.nuget.org) under [GitVersionTask](https://www.nuget.org/packages/GitVersionTask/)

    Install-Package GitVersionTask

Remove the `Assembly*Version` attributes from your `Properties\AssemblyInfo.cs` file. Sample default:

    [assembly: AssemblyVersion("1.0.0.0")]
    [assembly: AssemblyFileVersion("1.0.0.0")]
    [assembly: AssemblyInformationalVersion("1.0.0.0")]

Make sure there is a tag somewhere on master named `v1.2.3` before `HEAD` (change the numbers as desired).  Now when you build:

* AssemblyVersion will be set to 1.2.0.0 (i.e Major.Minor.0.0)
* AssemblyFileVersion will be set to 1.2.3.0 (i.e Major.Minor.Patch)
* AssemblyInformationalVersion will be set to `1.2.4+<commitcount>.Branch.<branchname>.Sha.<commithash>` where:
    * `<commitcount>` is the number of commits between the `v1.2.3` tag and `HEAD`.
    * `<branchname>` is the name of the branch you are on.
    * `<commithash>` is the commit hash of `HEAD`.

Continue working as usual and when you release/deploy, tag the branch/release `v1.2.4`.

If you want to bump up the major or minor version, create a text file in the root directory named NextVersion.txt and inside of it on a single line enter the version number that you want your next release to be.  e.g., `2.0`.

## Why is AssemblyVersion only set to Major.Minor?

This is a common approach that gives you the ability to roll out hot fixes to your assembly without breaking existing applications that may be referencing it. You are still able to get the full version number if you need to by looking at its file version number.

## My Git repository requires authentication. What do I do?

Set the environmental variables `GITVERSION_REMOTE_USERNAME` and `GITVERSION_REMOTE_PASSWORD` before the build is initiated.
