#addin "nuget:https://www.nuget.org/api/v2?package=Cake.Json&version=1.0.2.13"
#addin "nuget:https://www.nuget.org/api/v2?package=Cake.Docker&version=0.7.7"

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
		if (!artifactLookup.ContainsKey("NuGetExeDotNetCoreBuild")) { throw new Exception("NuGetExeDotNetCoreBuild artifact missing"); }
        if (!artifactLookup.ContainsKey("NuGetTaskBuild")) { throw new Exception("NuGetTaskBuild artifact missing"); }
        if (!artifactLookup.ContainsKey("NuGetExeBuild")) { throw new Exception("NuGetExeBuild artifact missing"); }
        if (!artifactLookup.ContainsKey("GemBuild")) { throw new Exception("GemBuild artifact missing"); }
        if (!artifactLookup.ContainsKey("GitVersionTfsTaskBuild")) { throw new Exception("GitVersionTfsTaskBuild artifact missing"); }
        if (!artifactLookup.ContainsKey("zip")) { throw new Exception("zip artifact missing"); }
		if (!artifactLookup.ContainsKey("zip-dotnetcore")) { throw new Exception("zip-dotnetcore artifact missing"); }		
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

Task("Publish-NuGetExeDotNetCore")
    .IsDependentOn("DownloadGitHubReleaseArtifacts")
    .Does(() =>
{
    NuGetPush(
        "./releaseArtifacts/" + artifactLookup["NuGetExeDotNetCoreBuild"],
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

// PublishDocker("gittools/gitversion", tag, "content.zip", "/some/path/DockerFile");	
bool PublishDocker(string name, tagName, contentZip, dockerFilePath, containerVolume)
{
    Information("Starting Docker Build for Image: " + name);

    var username = EnvironmentVariable("DOCKER_USERNAME");
    var password = EnvironmentVariable("DOCKER_PASSWORD");

	if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
    {
        Warning("Skipping docker publish due to missing credentials");
        return false;
    }

	// copy the docker file to a build directory, along with the contents of the specified content.zip.
	// This directory should then contain all we need for the docker build.
	var dockerBuildFolder = "./build/Docker/";
	CreateDirectory(dockerBuildFolder);

	//var folderName = name.Replace("/", "-");
	var dockerFileBuildFolder = dockerBuildFolder + name;
	CreateDirectory(dockerFileBuildFolder);
	
	Information("Copying docker file to " + dockerFileBuildFolder);	
	CopyFiles(dockerFilePath, dockerFileBuildFolder);

	var contentPath = "/content";
	var contentFolder = dockerFileBuildFolder + contentPath;

	Information("Extracting docker image content to " + contentFolder);
	Unzip(contentZip, contentFolder);		

	var dockerFilePathForBuild = dockerFileBuildFolder + "/DockerFile";
	Information("Beginning Docker Build command for " + dockerFilePathForBuild);

	var returnCode = StartProcess("docker", new ProcessSettings
    {
        Arguments = "build -f " + dockerFilePathForBuild + " " + dockerFileBuildFolder + " --build-arg contentFolder=" + contentPath + " --tag " + name + ":" + tagName
    });

    if (returnCode != 0) {
        Information("Publish-DockerImage Task failed to build image, but continuing with next Task...");
        publishingError = true;
        return false;
    }

    returnCode = StartProcess("docker", new ProcessSettings
    {
        Arguments = "run -v " + System.IO.Directory.GetCurrentDirectory() + ":" + containerVolume + " " + name + ":" + tag
    });

    if (returnCode != 0) {
        Information("Publish-DockerImage Task failed to run built image, but continuing with next Task...");
        publishingError = true;
        return false;
    }

    // Login to dockerhub
    returnCode = StartProcess("docker", new ProcessSettings
    {
        Arguments = "login -u=\"" + username +"\" -p=\"" + password +"\""
    });
    if (returnCode != 0) {
        Information("Publish-DockerImage Task failed to login, but continuing with next Task...");
        publishingError = true;
        return false;
    }

    // Publish Tag
    returnCode = StartProcess("docker", new ProcessSettings
    {
        Arguments = "push " + name + ":" + tag
    });
    if (returnCode != 0) {
        Information("Publish-DockerImage Task failed push version tag, but continuing with next Task...");
        publishingError = true;
        return false;
    }

    // Publish latest
    returnCode = StartProcess("docker", new ProcessSettings
    {
        Arguments = "tag " + name + ":" + tag + " " + name + ":latest"
    });
    if (returnCode != 0) {
        Information("Publish-DockerImage Task failed latest tag, but continuing with next Task...");
        publishingError = true;		
    }

    returnCode = StartProcess("docker", new ProcessSettings
    {
        Arguments = "push " + name + ":latest"
    });
    if (returnCode != 0) {
        Information("Publish-DockerImage Task failed latest tag, but continuing with next Task...");
        publishingError = true;
		return false;
    }

}

Task("Publish-DockerImage")
    .IsDependentOn("DownloadGitHubReleaseArtifacts")
    .Does(() =>
{            
   	PublishDocker("gittools/gitversion", tag, artifactLookup["zip"], "src/Docker/Mono/DockerFile", "/repo");	
	PublishDocker("gittools/gitversion-dotnetcore", tag, artifactLookup["zip-dotnetcore"], "src/Docker/DotNetCore/DockerFile", "c:/repo");	    
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