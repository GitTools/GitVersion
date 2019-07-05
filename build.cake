// Install modules
#module nuget:?package=Cake.DotNetTool.Module&version=0.2.0

// Install addins.
#addin "nuget:?package=Cake.Codecov&version=0.6.0"
#addin "nuget:?package=Cake.Coverlet&version=2.3.4"
#addin "nuget:?package=Cake.Docker&version=0.10.0"
#addin "nuget:?package=Cake.Gem&version=0.8.0"
#addin "nuget:?package=Cake.Gitter&version=0.11.0"
#addin "nuget:?package=Cake.Incubator&version=5.0.1"
#addin "nuget:?package=Cake.Json&version=3.0.0"
#addin "nuget:?package=Cake.Npm&version=0.17.0"
#addin "nuget:?package=Cake.Tfx&version=0.9.0"
#addin "nuget:?package=Cake.Gem&version=0.8.0"

#addin "nuget:?package=Newtonsoft.Json&version=9.0.1"
#addin "nuget:?package=xunit.assert&version=2.4.1"

// Install tools.
#tool "nuget:?package=vswhere&version=2.6.13-ga6d40ba5f4"
#tool "nuget:?package=NUnit.ConsoleRunner&version=3.10.0"
#tool "nuget:?package=ILRepack&version=2.0.16"
#tool "nuget:?package=Codecov&version=1.5.0"
#tool "nuget:?package=nuget.commandline&version=5.0.2"

// Install .NET Core Global tools.
#tool "dotnet:?package=GitReleaseManager.Tool&version=0.8.0"

// Load other scripts.
#load "./build/utils/parameters.cake"
#load "./build/utils/utils.cake"

#load "./build/pack.cake"
#load "./build/docker.cake"
#load "./build/publish.cake"

using Xunit;
using System.Diagnostics;
//////////////////////////////////////////////////////////////////////
// PARAMETERS
//////////////////////////////////////////////////////////////////////
bool publishingError = false;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup<BuildParameters>(context =>
{
    var parameters = BuildParameters.GetParameters(Context);
    Build(parameters.Configuration);
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

        if(context.Successful)
        {
            // if(parameters.ShouldPublish)
            // {
            //     if(parameters.CanPostToGitter)
            //     {
            //         var message = "@/all Version " + parameters.Version.SemVersion + " of the GitVersion has just been released, https://www.nuget.org/packages/GitVersion.";

            //         var postMessageResult = Gitter.Chat.PostMessage(
            //             message: message,
            //             messageSettings: new GitterChatMessageSettings { Token = parameters.Gitter.Token, RoomId = parameters.Gitter.RoomId}
            //         );

            //         if (postMessageResult.Ok)
            //         {
            //             Information("Message {0} succcessfully sent", postMessageResult.TimeStamp);
            //         }
            //         else
            //         {
            //             Error("Failed to send message: {0}", postMessageResult.Error);
            //         }
            //     }
            // }
        }

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
    .IsDependentOn("Pack-Vsix")
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

Task("Publish")
    .IsDependentOn("Publish-AppVeyor")
    .IsDependentOn("Publish-AzurePipeline")
    .IsDependentOn("Publish-Coverage")
    .IsDependentOn("Publish-NuGet")
    .IsDependentOn("Publish-Chocolatey")
    .IsDependentOn("Publish-Vsix")
    .IsDependentOn("Publish-Gem")
    .IsDependentOn("Publish-DockerHub")
    .Finally(() =>
{
    if (publishingError)
    {
        throw new Exception("An error occurred during the publishing of GitVersion. All publishing tasks have been attempted.");
    }
});

Task("Default")
    .IsDependentOn("Publish");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
RunTarget(target);
