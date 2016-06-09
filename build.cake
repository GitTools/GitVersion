#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=NUnit.ConsoleRunner"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

string version = null;
string nugetVersion = null;
string preReleaseTag = null;
string semVersion = null;
string milestone = null;
bool publishingError = false;
bool IsTagged = (BuildSystem.AppVeyor.Environment.Repository.Tag.IsTag &&
                !string.IsNullOrWhiteSpace(BuildSystem.AppVeyor.Environment.Repository.Tag.Name));
bool IsMainGitVersionRepo = StringComparer.OrdinalIgnoreCase.Equals("gittools/gitversion", BuildSystem.AppVeyor.Environment.Repository.Name);
bool IsPullRequest = BuildSystem.AppVeyor.Environment.PullRequest.IsPullRequest;

Setup(context =>
{
    if(!BuildSystem.IsLocalBuild)
    {
        GitVersion(new GitVersionSettings{
            UpdateAssemblyInfo = true,
            LogFilePath = "console",
            OutputType = GitVersionOutput.BuildServer
        });

        version = context.EnvironmentVariable("GitVersion_MajorMinorPatch");
        nugetVersion = context.EnvironmentVariable("GitVersion_NuGetVersion");
        preReleaseTag = context.EnvironmentVariable("GitVersion_PreReleaseTag");
        semVersion = context.EnvironmentVariable("GitVersion_LegacySemVerPadded");
        milestone = string.Concat("v", version);
    }

    GitVersion assertedVersions = GitVersion(new GitVersionSettings
    {
        OutputType = GitVersionOutput.Json
    });

    version = assertedVersions.MajorMinorPatch;
    nugetVersion = assertedVersions.NuGetVersion;
    preReleaseTag = assertedVersions.PreReleaseTag;
    semVersion = assertedVersions.LegacySemVerPadded;
    milestone = string.Concat("v", version);
});

Task("NuGet-Package-Restore")
    .Does(() =>
{
    NuGetRestore("./src/GitVersion.sln");
});

Task("Build")
    .IsDependentOn("NuGet-Package-Restore")
    .Does(() =>
{
    if(IsRunningOnUnix())
    {
        XBuild("./Source/Gep13.Cake.Sample.WebApplication.sln", new XBuildSettings()
            .SetConfiguration(configuration)
            .WithProperty("POSIX", "True")
            .SetVerbosity(Verbosity.Verbose)
        );
    }
    else
    {
        MSBuild("./src/GitVersion.sln", new MSBuildSettings()
            .SetConfiguration(configuration)
            .SetPlatformTarget(PlatformTarget.MSIL)
            .WithProperty("Windows", "True")
            .UseToolVersion(MSBuildToolVersion.VS2015)
            .SetVerbosity(Verbosity.Minimal)
            .SetNodeReuse(false));
    }
});

Task("Run-NUnit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    NUnit3("src/*.Tests/bin/" + configuration + "/*.Tests.dll");
});

Task("Zip-Files")
    .IsDependentOn("Run-NUnit-Tests")
    .Does(() =>
{
    var files = GetFiles("./build/NuGetCommandLineBuild/Tools/*.*");

    Zip("./", "GitVersion_" + nugetVersion + ".zip", files);

    files = GetFiles("./build/GitVersionTfsTaskBuild/GitVersionTask/*.*");

    Zip("./", "GitVersionTfsBuildTask_" + nugetVersion + ".zip", files);
});

Task("Create-NuGet-Packages")
    .Does(() =>
{

});

Task("Create-Chocolatey-Packages")
    .Does(() =>
{

});

Task("Create-Release-Notes")
    .Does(() =>
{
    //GitReleaseManagerCreate(parameters.GitHub.UserName, parameters.GitHub.Password, "cake-build", "cake", new GitReleaseManagerCreateSettings {
    //    Milestone         = parameters.Version.Milestone,
    //    Name              = parameters.Version.Milestone,
    //    Prerelease        = true,
    //    TargetCommitish   = "main"
    //});
});

Task("Package")
  .IsDependentOn("Zip-Files")
  .IsDependentOn("Create-NuGet-Packages")
  .IsDependentOn("Create-Chocolatey-Packages");

Task("Upload-AppVeyor-Artifacts")
    .IsDependentOn("Package")
    .WithCriteria(() => BuildSystem.AppVeyor.IsRunningOnAppVeyor)
    .Does(() =>
{
    AppVeyor.UploadArtifact("build/NuGetExeBuild/GitVersion.Portable." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/NuGetCommandLineBuild/GitVersion.CommandLine." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/NuGetRefBuild/GitVersion." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/NuGetTaskBuild/GitVersionTask." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/GitVersionTfsTaskBuild/gittools.gitversion-" + semVersion + ".vsix");
    AppVeyor.UploadArtifact("GitVersion_" + nugetVersion + ".zip");
    AppVeyor.UploadArtifact("GitVersionTfsBuildTask_" + nugetVersion + ".zip");
});

Task("Publish-MyGet")
    .IsDependentOn("Package")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
    .WithCriteria(() => !IsPullRequest)
    .WithCriteria(() => IsMainGitVersionRepo)
    .Does(() =>
{

})
.OnError(exception =>
{
    Information("Publish-MyGet Task failed, but continuing with next Task...");
    publishingError = true;
});

Task("Publish-NuGet")
    .IsDependentOn("Package")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
    .WithCriteria(() => !IsPullRequest)
    .WithCriteria(() => IsMainGitVersionRepo)
    .WithCriteria(() => IsTagged)
    .Does(() =>
{

})
.OnError(exception =>
{
    Information("Publish-NuGet Task failed, but continuing with next Task...");
    publishingError = true;
});

Task("Publish-Chocolatey")
    .IsDependentOn("Package")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
    .WithCriteria(() => !IsPullRequest)
    .WithCriteria(() => IsMainGitVersionRepo)
    .WithCriteria(() => IsTagged)
    .Does(() =>
{

})
.OnError(exception =>
{
    Information("Publish-Chocolatey Task failed, but continuing with next Task...");
    publishingError = true;
});

Task("Publish-Gem")
    .IsDependentOn("Package")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
    .WithCriteria(() => !IsPullRequest)
    .WithCriteria(() => IsMainGitVersionRepo)
    .WithCriteria(() => IsTagged)
    .Does(() =>
{

})
.OnError(exception =>
{
    Information("Publish-Gem Task failed, but continuing with next Task...");
    publishingError = true;
});

Task("Publish-GitHub-Release")
    .IsDependentOn("Package")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
    .WithCriteria(() => !IsPullRequest)
    .WithCriteria(() => IsMainGitVersionRepo)
    .WithCriteria(() => IsTagged)
    .Does(() =>
{

})
.OnError(exception =>
{
    Information("Publish-GitHub-Release Task failed, but continuing with next Task...");
    publishingError = true;
});

Task("AppVeyor")
  .IsDependentOn("Upload-AppVeyor-Artifacts")
  .IsDependentOn("Publish-MyGet")
  .IsDependentOn("Publish-NuGet")
  .IsDependentOn("Publish-Chocolatey")
  .IsDependentOn("Publish-Gem")
  .IsDependentOn("Publish-GitHub-Release")
  .Finally(() =>
{
    if(publishingError)
    {
        throw new Exception("An error occurred during the publishing of Cake.  All publishing tasks have been attempted.");
    }
});

Task("Travis")
  .IsDependentOn("Run-NUnit-Tests");

Task("ReleaseNotes")
  .IsDependentOn("Create-Release-Notes");

Task("Default")
  .IsDependentOn("Package");

RunTarget(target);