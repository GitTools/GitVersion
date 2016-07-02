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

void Build()
{
    if(IsRunningOnUnix())
    {
        XBuild("./src/GitVersion.sln", new XBuildSettings()
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
}

Task("DogfoodBuild")
    .IsDependentOn("NuGet-Package-Restore")
    .Does(() =>
{
    Build();
});

Task("Version")
    .IsDependentOn("DogfoodBuild")
    .Does(() =>
{
    if(!BuildSystem.IsLocalBuild)
    {
        GitVersion(new GitVersionSettings
        {
            UpdateAssemblyInfo = true,
            LogFilePath = "console",
            OutputType = GitVersionOutput.BuildServer,
            ToolPath = @"src\GitVersionExe\bin\Release\GitVersion.exe"
        });

        version = EnvironmentVariable("GitVersion_MajorMinorPatch");
        nugetVersion = EnvironmentVariable("GitVersion_NuGetVersion");
        preReleaseTag = EnvironmentVariable("GitVersion_PreReleaseTag");
        semVersion = EnvironmentVariable("GitVersion_LegacySemVerPadded");
        milestone = string.Concat("v", version);
    }

    GitVersion assertedVersions = GitVersion(new GitVersionSettings
    {
        OutputType = GitVersionOutput.Json,
        ToolPath = @"src\GitVersionExe\bin\Release\GitVersion.exe"
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
    .IsDependentOn("Version")
    .IsDependentOn("NuGet-Package-Restore")
    .Does(() =>
{
    Build();
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
    Zip("./build/NuGetCommandLineBuild/Tools/", "GitVersion_" + nugetVersion + ".zip");
});

Task("Create-Release-Notes")
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
    .WithCriteria(() => BuildSystem.AppVeyor.IsRunningOnAppVeyor)
    .Does(() =>
{
    System.IO.File.WriteAllLines("build/artifacts", new[]{
        "NuGetExeBuild:GitVersion.Portable." + nugetVersion +".nupkg",
        "NuGetCommandLineBuild:GitVersion.CommandLine." + nugetVersion +".nupkg",
        "NuGetRefBuild:GitVersion." + nugetVersion +".nupkg",
        "NuGetTaskBuild:GitVersionTask." + nugetVersion +".nupkg",
        "NuGetExeBuild:GitVersion.Portable." + nugetVersion +".nupkg",
        "zip:GitVersion_" + nugetVersion + ".zip",
        "releaseNotes:releasenotes.md"
    });

    AppVeyor.UploadArtifact("build/NuGetExeBuild/GitVersion.Portable." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/NuGetCommandLineBuild/GitVersion.CommandLine." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/NuGetRefBuild/GitVersion." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/NuGetTaskBuild/GitVersionTask." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/GitVersionTfsTaskBuild/gittools.gitversion-" + semVersion + ".vsix");
    AppVeyor.UploadArtifact("build/GitVersion_" + nugetVersion + ".zip");
    AppVeyor.UploadArtifact("build/GitVersionTfsBuildTask_" + nugetVersion + ".zip");
});


Task("Travis")
  .IsDependentOn("Run-NUnit-Tests");

Task("Default")
  .IsDependentOn("Upload-AppVeyor-Artifacts");

RunTarget(target);