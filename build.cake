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
            .WithProperty("Windows", "True")
            .UseToolVersion(MSBuildToolVersion.VS2015)
            .SetVerbosity(Verbosity.Minimal)
            .SetNodeReuse(false);

        if (BuildSystem.AppVeyor.IsRunningOnAppVeyor)
        {
            msBuildSettings = msBuildSettings
                .WithProperty("GitVersion_NuGetVersion", nugetVersion)
                .WithProperty("GitVersion_SemVer", semVersion)
                .WithProperty("GitVersion_MajorMinorPatch", version)
                .WithProperty("GitVersion_PreReleaseTag", preReleaseTag);
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
        ToolPath = @"src\GitVersionExe\bin\Release\GitVersion.exe"
    });
    GitVersion assertedVersions = GitVersion(new GitVersionSettings
    {
        OutputType = GitVersionOutput.Json,
        ToolPath = @"src\GitVersionExe\bin\Release\GitVersion.exe"
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
    var settings = new NUnit3Settings();
    if(IsRunningOnUnix())
    {
        settings.Where = "cat != NoMono";
    }
    NUnit3(new [] {
        "src/GitVersionCore.Tests/bin/" + configuration + "/GitVersionCore.Tests.dll",
        "src/GitVersionExe.Tests/bin/" + configuration + "/GitVersionExe.Tests.dll",
        "src/GitVersionTask.Tests/bin/" + configuration + "/GitVersionTask.Tests.dll" },
        settings);
    if (AppVeyor.IsRunningOnAppVeyor)
    {
        Information("Uploading test results");
        AppVeyor.UploadTestResults("TestResult.xml", AppVeyorTestResultsType.NUnit3);
    }
});

Task("Zip-Files")
    .IsDependentOn("Run-NUnit-Tests")
    .Does(() =>
{
    Zip("./build/NuGetCommandLineBuild/Tools/", "build/GitVersion_" + nugetVersion + ".zip");
});

Task("Create-Release-Notes")
    .IsDependentOn("Build")
    .Does(() =>
{
    var releaseNotesExitCode = StartProcess(
            @"tools\GitReleaseNotes\tools\gitreleasenotes.exe",
            new ProcessSettings { Arguments = ". /o build/releasenotes.md" });
    if (string.IsNullOrEmpty(System.IO.File.ReadAllText("./build/releasenotes.md")))
        System.IO.File.WriteAllText("./build/releasenotes.md", "No issues closed since last release");

    if (releaseNotesExitCode != 0) throw new Exception("Failed to generate release notes");
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
        "zip:GitVersion_" + nugetVersion + ".zip",
        "releaseNotes:releasenotes.md"
    });

    AppVeyor.UploadArtifact("build/NuGetExeBuild/GitVersion.Portable." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/NuGetCommandLineBuild/GitVersion.CommandLine." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/NuGetRefBuild/GitVersion." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/NuGetTaskBuild/GitVersionTask." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/GitVersionTfsTaskBuild/gittools.gitversion-" + semVersion + ".vsix");
    AppVeyor.UploadArtifact("build/GitVersion_" + nugetVersion + ".zip");
    AppVeyor.UploadArtifact("build/GemBuild/" + gem);
    AppVeyor.UploadArtifact("build/releasenotes.md");
    AppVeyor.UploadArtifact("build/artifacts");
});


Task("Travis")
  .IsDependentOn("Run-NUnit-Tests");

Task("Default")
  .IsDependentOn("Upload-AppVeyor-Artifacts");

RunTarget(target);