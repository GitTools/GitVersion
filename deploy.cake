#addin "Cake.Json"
#addin "Cake.Docker"

var target = Argument("target", "Deploy");
var tagOverride = Argument("TagOverride", "");

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
        // This allows us to test deployments locally..
        if (!string.IsNullOrWhiteSpace(tagOverride))
        {
            tag = tagOverride;
            return;
        }

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
        // Will not be empty if overriden
        if (tag == "")
        {
            tag = AppVeyor.Environment.Repository.Tag.Name;
            AppVeyor.UpdateBuildVersion(tag);
        }
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

        // Have had missing artifacts before, lets fail early in that scenario
        if (!artifactLookup.ContainsKey("NuGetRefBuild")) { throw new Exception("NuGetRefBuild artifact missing"); }
        if (!artifactLookup.ContainsKey("NuGetCommandLineBuild")) { throw new Exception("NuGetCommandLineBuild artifact missing"); }
        if (!artifactLookup.ContainsKey("NuGetTaskBuild")) { throw new Exception("NuGetTaskBuild artifact missing"); }
        if (!artifactLookup.ContainsKey("NuGetExeBuild")) { throw new Exception("NuGetExeBuild artifact missing"); }
        if (!artifactLookup.ContainsKey("GemBuild")) { throw new Exception("GemBuild artifact missing"); }
        if (!artifactLookup.ContainsKey("GitVersionTfsTaskBuild")) { throw new Exception("GitVersionTfsTaskBuild artifact missing"); }
        if (!artifactLookup.ContainsKey("zip")) { throw new Exception("zip artifact missing"); }
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
    .WithCriteria(() => !tag.Contains("-")) // Do not release pre-release to VSTS
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


Task("Publish-DockerImage")
    .IsDependentOn("DownloadGitHubReleaseArtifacts")
    .Does(() =>
{
    var username = EnvironmentVariable("DOCKER_USERNAME");
    var password = EnvironmentVariable("DOCKER_PASSWORD");
    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
    {
        Warning("Skipping docker publish due to missing credentials");
        return;
    }

    var returnCode = StartProcess("docker", new ProcessSettings
    {
        Arguments = "build . --build-arg GitVersionZip=" + artifactLookup["zip"] + " --tag gittools/gitversion:" + tag
    });
    if (returnCode != 0) {
        Information("Publish-DockerImage Task failed to build image, but continuing with next Task...");
        publishingError = true;
        return;
    }

    returnCode = StartProcess("docker", new ProcessSettings
    {
        Arguments = "run -v " + System.IO.Directory.GetCurrentDirectory() + ":/repo gittools/gitversion:" + tag
    });
    if (returnCode != 0) {
        Information("Publish-DockerImage Task failed to run built image, but continuing with next Task...");
        publishingError = true;
        return;
    }
    
    // Login to dockerhub
    returnCode = StartProcess("docker", new ProcessSettings
    {
        Arguments = "login -u=\"" + username +"\" -p=\"" + password +"\""
    });
    if (returnCode != 0) {
        Information("Publish-DockerImage Task failed to login, but continuing with next Task...");
        publishingError = true;
        return;
    }

    // Publish Tag
    returnCode = StartProcess("docker", new ProcessSettings
    {
        Arguments = "push gittools/gitversion:" + tag
    });
    if (returnCode != 0) {
        Information("Publish-DockerImage Task failed push version tag, but continuing with next Task...");
        publishingError = true;
        return;
    }

    // Publish latest
    returnCode = StartProcess("docker", new ProcessSettings
    {
        Arguments = "tag gittools/gitversion:" + tag + " gittools/gitversion:latest"
    });
    if (returnCode != 0) {
        Information("Publish-DockerImage Task failed latest tag, but continuing with next Task...");
        publishingError = true;
    }
    returnCode = StartProcess("docker", new ProcessSettings
    {
        Arguments = "push gittools/gitversion:latest"
    });
    if (returnCode != 0) {
        Information("Publish-DockerImage Task failed latest tag, but continuing with next Task...");
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
  .IsDependentOn("Publish-DockerImage")
  .Finally(() =>
{
    if(publishingError)
    {
        throw new Exception("An error occurred during the publishing of Cake.  All publishing tasks have been attempted.");
    }
});

RunTarget(target);