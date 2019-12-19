// Install modules
#module nuget:?package=Cake.DotNetTool.Module&version=0.4.0

// Install addins.
#addin "nuget:?package=Cake.Codecov&version=0.7.0"
#addin "nuget:?package=Cake.Compression&version=0.2.4"
#addin "nuget:?package=Cake.Coverlet&version=2.3.4"
#addin "nuget:?package=Cake.Docker&version=0.10.1"
#addin "nuget:?package=Cake.Gem&version=0.8.1"
#addin "nuget:?package=Cake.Git&version=0.21.0"
#addin "nuget:?package=Cake.Gitter&version=0.11.1"
#addin "nuget:?package=Cake.Incubator&version=5.1.0"
#addin "nuget:?package=Cake.Json&version=4.0.0"
#addin "nuget:?package=Cake.Kudu&version=0.10.1"
#addin "nuget:?package=Cake.Npm&version=0.17.0"
#addin "nuget:?package=Cake.Tfx&version=0.9.1"
#addin "nuget:?package=Cake.Wyam&version=2.2.9"

#addin "nuget:?package=Newtonsoft.Json&version=12.0.2"
#addin "nuget:?package=SharpZipLib&version=1.2.0"
#addin "nuget:?package=xunit.assert&version=2.4.1"

// Install tools.
#tool "nuget:?package=NUnit.ConsoleRunner&version=3.10.0"
#tool "nuget:?package=nuget.commandline&version=5.2.0"
#tool "nuget:?package=Wyam&version=2.2.9"
#tool "nuget:?package=KuduSync.NET&version=1.5.2"

// Install .NET Core Global tools.
#tool "dotnet:?package=Codecov.Tool&version=1.7.2"
#tool "dotnet:?package=GitReleaseManager.Tool&version=0.9.0"

// Load other scripts.
#load "./build/utils/parameters.cake"
#load "./build/utils/utils.cake"

#load "./build/test.cake"
#load "./build/pack.cake"
#load "./build/artifacts-test.cake"
#load "./build/docker.cake"
#load "./build/publish.cake"
#load "./build/wyam.cake"

using Xunit;
using System.Diagnostics;
//////////////////////////////////////////////////////////////////////
// PARAMETERS
//////////////////////////////////////////////////////////////////////
bool publishingError = false;
bool singleStageRun = true;
///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup<BuildParameters>(context =>
{
    try
    {
        EnsureDirectoryExists("artifacts");
        var parameters = BuildParameters.GetParameters(context);
        var gitVersion = GetVersion(parameters);
        parameters.Initialize(context, gitVersion);

        // Increase verbosity?
        if (parameters.IsMainBranch && (context.Log.Verbosity != Verbosity.Diagnostic)) {
            Information("Increasing verbosity to diagnostic.");
            context.Log.Verbosity = Verbosity.Diagnostic;
        }

        Information("Building version {0} of GitVersion ({1}, {2})",
            parameters.Version.SemVersion,
            parameters.Configuration,
            parameters.Target);

        Information("Repository info : IsMainRepo {0}, IsMainBranch {1}, IsTagged: {2}, IsPullRequest: {3}",
            parameters.IsMainRepo,
            parameters.IsMainBranch,
            parameters.IsTagged,
            parameters.IsPullRequest);

        return parameters;
    }
    catch (Exception exception)
    {
        Error(exception.Dump());
        return null;
    }
});

Teardown<BuildParameters>((context, parameters) =>
{
    try
    {
        Information("Starting Teardown...");

        Information("Repository info : IsMainRepo {0}, IsMainBranch {1}, IsTagged: {2}, IsPullRequest: {3}",
            parameters.IsMainRepo,
            parameters.IsMainBranch,
            parameters.IsTagged,
            parameters.IsPullRequest);

        Information("Finished running tasks.");
    }
    catch (Exception exception)
    {
        Error(exception.Dump());
    }
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Pack")
    .IsDependentOn("Pack-Gem")
    .IsDependentOn("Pack-Nuget")
    .IsDependentOn("Pack-Chocolatey")
    .IsDependentOn("Zip-Files")
    .Does<BuildParameters>((parameters) =>
{
    Information("The build artifacts: \n");
    foreach(var artifact in parameters.Artifacts.All)
    {
        if (FileExists(artifact.ArtifactPath)) { Information("Artifact: {0}", artifact.ArtifactPath); }
    }

    foreach(var package in parameters.Packages.All)
    {
        if (FileExists(package.PackagePath)) { Information("Artifact: {0}", package.PackagePath); }
    }
})
    .ReportError(exception =>
{
    Error(exception.Dump());
});

Task("Test")
    .IsDependentOn("Publish-Coverage")
    .Finally(() =>
{
});

Task("Publish")
    .IsDependentOn("Publish-AppVeyor")
    .IsDependentOn("Publish-AzurePipeline")
    .IsDependentOn("Publish-NuGet")
    .IsDependentOn("Publish-Chocolatey")
    .IsDependentOn("Publish-Gem")
    .IsDependentOn("Publish-Documentation")
    .Finally(() =>
{
    if (publishingError)
    {
        throw new Exception("An error occurred during the publishing of GitVersion. All publishing tasks have been attempted.");
    }
});

Task("Publish-DockerHub")
    .IsDependentOn("Docker-Publish")
    .Finally(() =>
{
    if (publishingError)
    {
        throw new Exception("An error occurred during the publishing of GitVersion. All publishing tasks have been attempted.");
    }
});

Task("Release")
    .IsDependentOn("Release-Notes")
    .Finally(() =>
{
});

Task("Default")
    .IsDependentOn("Publish")
    .IsDependentOn("Publish-DockerHub")
    .IsDependentOn("Release");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
RunTarget(target);
