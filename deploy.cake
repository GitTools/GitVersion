#addin "Cake.Json"

var target = Argument("target", "Deploy");

using System.Net;
using System.Linq;
using System.Collections.Generic;

string Get(string url)
{
    var assetsRequest = WebRequest.CreateHttp(url);
    assetsRequest.Method = "GET";
    assetsRequest.Accept = "application/vnd.github.v3+json";
    assetsRequest.UserAgent = "BuildScript";

    using (var assetsResponse = assetsRequest.GetResponse())
    {
        var assetsStream = assetsResponse.GetResponseStream();
        var assetsReader = new StreamReader(assetsStream);
        var assetsBody = assetsReader.ReadToEnd();
        return assetsBody;
    }
}

Task("EnsureRequirements")
    .Does(() =>
    {
        if (!AppVeyor.IsRunningOnAppVeyor)
           throw new Exception("Deployment should happen via appveyor");

        var isTag =
           AppVeyor.Environment.Repository.Tag.IsTag &&
           !string.IsNullOrWhiteSpace(AppVeyor.Environment.Repository.Tag.Name);
        if (!isTag)
           throw new Exception("Deployment should happen from a published GitHub release");
    });

var tag = "";
Dictionary<string, string> artifactLookup = null;
var publishingError = false;
Task("UpdateVersionInfo")
    .IsDependentOn("EnsureRequirements")
    .Does(() =>
    {
        tag = AppVeyor.Environment.Repository.Tag.Name;
        AppVeyor.UpdateBuildVersion(tag);
    });

Task("DownloadGitHubReleaseArtifacts")
    .IsDependentOn("UpdateVersionInfo")
    .Does(() =>
    {
        var assets_url = ParseJson(Get("https://api.github.com/repos/GitTools/GitVersion/releases/tags/" + tag))
            .GetValue("assets_url").Value<string>();
        EnsureDirectoryExists("./releaseArtifacts");
        foreach(var asset in DeserializeJson<JArray>(Get(assets_url)))
        {
            DownloadFile(asset.Value<string>("browser_download_url"), "./releaseArtifacts/" + asset.Value<string>("name"));
        }

        // Turns .artifacts file into a lookup
        artifactLookup = System.IO.File
            .ReadAllLines("./releaseArtifacts/artifacts")
            .Select(l => l.Split(':'))
            .ToDictionary(v => v[0], v => v[1]);
    });

Task("Publish-NuGetPackage")
    .IsDependentOn("DownloadGitHubReleaseArtifacts")
    .Does(() =>
{
    NuGetPush(
        "./releaseArtifacts/" + artifactLookup["NuGetRefBuild"],
        new NuGetPushSettings {
            ApiKey = EnvironmentVariable("NuGetApiKey"),
            Source = "https://www.nuget.org/api/v2/package"
        });
})
.OnError(exception =>
{
    Information("Publish-NuGet Task failed, but continuing with next Task...");
    publishingError = true;
});

Task("Publish-NuGetCommandLine")
    .IsDependentOn("DownloadGitHubReleaseArtifacts")
    .Does(() =>
{
    NuGetPush(
        "./releaseArtifacts/" + artifactLookup["NuGetCommandLineBuild"],
        new NuGetPushSettings {
            ApiKey = EnvironmentVariable("NuGetApiKey"),
            Source = "https://www.nuget.org/api/v2/package"
        });
})
.OnError(exception =>
{
    Information("Publish-NuGet Task failed, but continuing with next Task...");
    publishingError = true;
});


Task("Publish-MsBuildTask")
    .IsDependentOn("DownloadGitHubReleaseArtifacts")
    .Does(() =>
{
    NuGetPush(
        "./releaseArtifacts/" + artifactLookup["NuGetTaskBuild"],
        new NuGetPushSettings {
            ApiKey = EnvironmentVariable("NuGetApiKey"),
            Source = "https://www.nuget.org/api/v2/package"
        });
})
.OnError(exception =>
{
    Information("Publish-NuGet Task failed, but continuing with next Task...");
    publishingError = true;
});

Task("Publish-Chocolatey")
    .IsDependentOn("DownloadGitHubReleaseArtifacts")
    .Does(() =>
{
    NuGetPush(
        "./releaseArtifacts/" + artifactLookup["NuGetExeBuild"],
        new NuGetPushSettings {
            ApiKey = EnvironmentVariable("ChocolateyApiKey"),
            Source = "https://chocolatey.org/api/v2/package"
        });
})
.OnError(exception =>
{
    Information("Publish-Chocolatey Task failed, but continuing with next Task...");
    publishingError = true;
});

Task("Publish-Gem")
    .IsDependentOn("DownloadGitHubReleaseArtifacts")
    .Does(() =>
{
    var returnCode = StartProcess("cmd", new ProcessSettings
    {
        Arguments = " /c gem push ./releaseArtifacts/" + artifactLookup["GemBuild"] + " --key " + EnvironmentVariable("GemApiKey") + " && exit 0 || exit 1"
    });

    if (returnCode != 0) {
        Information("Publish-Gem Task failed, but continuing with next Task...");
        publishingError = true;
    }
});


Task("Publish-VstsTask")
    .IsDependentOn("DownloadGitHubReleaseArtifacts")
    .Does(() =>
{
    var returnCode = StartProcess("cmd", new ProcessSettings
    {
        Arguments = " /c tfx extension publish --vsix ./releaseArtifacts/" + artifactLookup["GitVersionTfsTaskBuild"] + " --no-prompt --auth-type pat --token " + EnvironmentVariable("MarketplaceApiKey") + " && exit 0 || exit 1"
    });

    if (returnCode != 0) {
        Information("Publish-VstsTask Task failed, but continuing with next Task...");
        publishingError = true;
    }
});

Task("Deploy")
  .IsDependentOn("Publish-NuGetPackage")
  .IsDependentOn("Publish-NuGetCommandLine")
  .IsDependentOn("Publish-MsBuildTask")
  .IsDependentOn("Publish-Chocolatey")
//  .IsDependentOn("Publish-Gem")
  .IsDependentOn("Publish-VstsTask")
  .Finally(() =>
{
    if(publishingError)
    {
        throw new Exception("An error occurred during the publishing of Cake.  All publishing tasks have been attempted.");
    }
});

RunTarget(target);