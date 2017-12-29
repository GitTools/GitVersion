#tool "nuget:?package=NUnit.ConsoleRunner"
#tool "nuget:?package=GitReleaseNotes"

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
bool IsMainGitVersionBranch = StringComparer.OrdinalIgnoreCase.Equals("master", BuildSystem.AppVeyor.Environment.Repository.Branch);

void Build(string configuration, string nugetVersion, string semVersion, string version, string preReleaseTag)
{
    if(IsRunningOnUnix())
    {
        XBuild("./src/GitVersion.sln",  new XBuildSettings()
            .SetConfiguration(configuration)
            .WithProperty("POSIX", "True")
            .SetVerbosity(Verbosity.Minimal));
    }
    else
    {	

        var msBuildSettings = new MSBuildSettings()
            .SetConfiguration(configuration)		
            .SetPlatformTarget(PlatformTarget.MSIL)
		   // .WithProperty("Platform", "Any CPU")
           // .WithProperty("Windows", "True")
            .UseToolVersion(MSBuildToolVersion.VS2017)
            .SetVerbosity(Verbosity.Minimal)
            .SetNodeReuse(false);

        if (BuildSystem.AppVeyor.IsRunningOnAppVeyor)
		{
			if (!string.IsNullOrWhiteSpace(nugetVersion))
			{
				msBuildSettings.WithProperty("GitVersion_NuGetVersion", nugetVersion);
			}
			if (!string.IsNullOrWhiteSpace(semVersion))
			{
				msBuildSettings.WithProperty("GitVersion_SemVer", semVersion);
			}

			if (!string.IsNullOrWhiteSpace(version))
			{
				msBuildSettings.WithProperty("GitVersion_MajorMinorPatch", version);
			}

			if (!string.IsNullOrWhiteSpace(preReleaseTag))
			{
				msBuildSettings.WithProperty("GitVersion_PreReleaseTag", preReleaseTag);
			}
        }

        MSBuild("./src/GitVersion.sln", msBuildSettings);
    }
}

Task("DogfoodBuild")
    .IsDependentOn("NuGet-Package-Restore")
    .Does(() =>
{
    Build(configuration, nugetVersion, semVersion, version, preReleaseTag);
});

Task("Version")
    .IsDependentOn("DogfoodBuild")
    .Does(() =>
{
    GitVersion(new GitVersionSettings
    {
        UpdateAssemblyInfo = true,
        LogFilePath = "console",
        OutputType = GitVersionOutput.BuildServer,
        ToolPath = @"src\GitVersionExe\bin\Release\net40\GitVersion.exe"
    });
    GitVersion assertedVersions = GitVersion(new GitVersionSettings
    {
        OutputType = GitVersionOutput.Json,
        ToolPath = @"src\GitVersionExe\bin\Release\net40\GitVersion.exe"
    });

    version = assertedVersions.MajorMinorPatch;
    nugetVersion = assertedVersions.NuGetVersion;
    preReleaseTag = assertedVersions.PreReleaseTag;
    semVersion = assertedVersions.LegacySemVerPadded;
});

Task("NuGet-Package-Restore")
    .Does(() =>
{
    NuGetRestore("./src/GitVersion.sln");
});

Task("Build")
    .IsDependentOn("Version")
    .IsDependentOn("NuGet-Package-Restore")
    .Does(() =>
{
    Build(configuration, nugetVersion, semVersion, version, preReleaseTag);
});

Task("Run-NUnit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{

     var settings = new DotNetCoreTestSettings
     {
         Configuration = "Release",
		 NoBuild = true
     };

     DotNetCoreTest("./src/GitVersionCore.Tests/GitVersionCore.Tests.csproj", settings);
	 DotNetCoreTest("./src/GitVersionExe.Tests/GitVersionExe.Tests.csproj", settings);
	 DotNetCoreTest("./src/GitVersionTask.Tests/GitVersionTask.Tests.csproj", settings);
    
});

Task("Zip-Files")
    .IsDependentOn("Run-NUnit-Tests")
    .Does(() =>
{
    Zip("./build/NuGetCommandLineBuild/Tools/", "build/GitVersion_" + nugetVersion + ".zip");
});

Task("Create-Release-Notes")
    .IsDependentOn("Build")
    .WithCriteria(() => IsMainGitVersionRepo && IsMainGitVersionBranch && !IsPullRequest)
    .Does(() =>
{
    var githubToken = EnvironmentVariable("GitHubToken");

    if(!string.IsNullOrWhiteSpace(githubToken))
    {
        IEnumerable<string> redirectedOutput;
        var releaseNotesExitCode = StartProcess(
                @"tools\GitReleaseNotes\tools\gitreleasenotes.exe",
                new ProcessSettings {
                    Arguments = ". /o build/releasenotes.md /repoToken " + githubToken,
                    RedirectStandardOutput = true
                },
                out redirectedOutput);
        Information(string.Join("\n", redirectedOutput));

        if (!System.IO.File.Exists("./build/releasenotes.md") || string.IsNullOrEmpty(System.IO.File.ReadAllText("./build/releasenotes.md"))) {
            System.IO.File.WriteAllText("./build/releasenotes.md", "No issues closed since last release");
        }
    }
    else
    {
        Information("Create-Release-Notes is being skipped, as GitHub Token is not present.");
    }
});

Task("Package")
    .IsDependentOn("Create-Release-Notes")
    .IsDependentOn("Zip-Files");

Task("Upload-AppVeyor-Artifacts")
    .IsDependentOn("Package")
    .WithCriteria(() => AppVeyor.IsRunningOnAppVeyor)
    .Does(() =>
{
    var gem = string.IsNullOrEmpty(preReleaseTag) ?
        "gitversion-" + version + ".gem" :
        "gitversion-" + version + "." + preReleaseTag + ".gem";

    System.IO.File.WriteAllLines("build/artifacts", new[]{
        "NuGetExeBuild:GitVersion.Portable." + nugetVersion +".nupkg",
        "NuGetCommandLineBuild:GitVersion.CommandLine." + nugetVersion +".nupkg",
        "NuGetRefBuild:GitVersion." + nugetVersion +".nupkg",
        "NuGetTaskBuild:GitVersionTask." + nugetVersion +".nupkg",
        "GitVersionTfsTaskBuild:gittools.gitversion-" + semVersion +".vsix",
        "GemBuild:" + gem,
        "zip:GitVersion_" + nugetVersion + ".zip"
    });

    AppVeyor.UploadArtifact("build/NuGetExeBuild/GitVersion.Portable." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/NuGetCommandLineBuild/GitVersion.CommandLine." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/NuGetRefBuild/GitVersion." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/NuGetTaskBuild/GitVersionTask." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/GitVersionTfsTaskBuild/gittools.gitversion-" + semVersion + ".vsix");
    AppVeyor.UploadArtifact("build/GitVersion_" + nugetVersion + ".zip");
    AppVeyor.UploadArtifact("build/GemBuild/" + gem);
    AppVeyor.UploadArtifact("build/artifacts");

    if(IsMainGitVersionRepo && IsMainGitVersionBranch && !IsPullRequest)
    {
        if(FileExists("build/releasenotes.md"))
        {
            AppVeyor.UploadArtifact("build/releasenotes.md");
        }
    }
});


Task("Travis")
  .IsDependentOn("Run-NUnit-Tests");

Task("Default")
  .IsDependentOn("Upload-AppVeyor-Artifacts");

RunTarget(target);