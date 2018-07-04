#tool "nuget:https://www.nuget.org/api/v2?package=NUnit.ConsoleRunner&version=3.7.0"
#tool "nuget:https://www.nuget.org/api/v2?package=GitReleaseNotes&version=0.7.1"
#tool "nuget:https://www.nuget.org/api/v2?package=ILRepack&version=2.0.15"
#addin "nuget:?package=Cake.Incubator"

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

string buildDir = "./build/";

void Build(string configuration, string nugetVersion, string semVersion, string version, string preReleaseTag)
{

    MSBuild("./src/GitVersion.sln", settings =>
	{
	 settings.SetConfiguration(configuration)
        .SetVerbosity(Verbosity.Minimal)
        .WithTarget("Build")
        .WithProperty("POSIX",IsRunningOnUnix().ToString());
		
		if (BuildSystem.AppVeyor.IsRunningOnAppVeyor)
		{
			if (!string.IsNullOrWhiteSpace(nugetVersion))
			{
				settings.WithProperty("GitVersion_NuGetVersion", nugetVersion);
			}
			if (!string.IsNullOrWhiteSpace(semVersion))
			{
				settings.WithProperty("GitVersion_SemVer", semVersion);
			}

			if (!string.IsNullOrWhiteSpace(version))
			{
				settings.WithProperty("GitVersion_MajorMinorPatch", version);
			}

			if (!string.IsNullOrWhiteSpace(preReleaseTag))
			{
				settings.WithProperty("GitVersion_PreReleaseTag", preReleaseTag);
			}
        }		
	}); 
}

// This build task can be run to just build
Task("DogfoodBuild")
    .IsDependentOn("Clean")
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
	DotNetCoreRestore("./src/GitVersion.sln");
    //NuGetRestore("./src/GitVersion.sln");
});

Task("Clean")   
    .Does(() =>
{
	CleanDirectories("./build");    
	CleanDirectories("./**/obj"); 
	
	var binDirs = GetDirectories("./**/bin") - GetDirectories("**/GemAssets/bin");
	CleanDirectories(binDirs);  
});

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Version")
    .IsDependentOn("NuGet-Package-Restore")
    .Does(() =>
{
    Build(configuration, nugetVersion, semVersion, version, preReleaseTag);
});

Task("Run-Tests-In-NUnitConsole")   
    .IsDependentOn("DogfoodBuild")
    .Does(() =>
{
    var settings = new NUnit3Settings();
	var targetFramework = "net461";  
    if(IsRunningOnUnix())
    {
        settings.Where = "cat != NoMono";
    }

    NUnit3(new [] {
        "src/GitVersionCore.Tests/bin/" + configuration + "/" + targetFramework + "/GitVersionCore.Tests.dll",
        "src/GitVersionExe.Tests/bin/" + configuration + "/" + targetFramework + "/GitVersionExe.Tests.dll",
        "src/GitVersionTask.Tests/bin/" + configuration + "/" + targetFramework + "/GitVersionTask.Tests.dll" },
        settings);
    if (AppVeyor.IsRunningOnAppVeyor)
    {
        Information("Uploading test results");
        AppVeyor.UploadTestResults("TestResult.xml", AppVeyorTestResultsType.NUnit3);
    }
});

// Note: this task is not used for time being as unable to produce sensible test results output file via dotnet cli.. so using nunit console runner above instead.
Task("Run-Tests")  
    .IsDependentOn("DogfoodBuild")
    .Does(() =>
{	
     var settings = new DotNetCoreTestSettings
     {
        Configuration = configuration,
        NoBuild = true,
        Filter = "TestCategory!=NoMono"		
     };

     DotNetCoreTest("./src/GitVersionCore.Tests/GitVersionCore.Tests.csproj", settings);
	 DotNetCoreTest("./src/GitVersionExe.Tests/GitVersionExe.Tests.csproj", settings);
	 DotNetCoreTest("./src/GitVersionTask.Tests/GitVersionTask.Tests.csproj", settings);
    
});

void ILRepackGitVersionExe(bool includeLibGit2Sharp)
{
	 var tempMergeDir = "ILMergeTemp";
	 var exeName = "GitVersion.exe";
	 var keyFilePath = "./src/key.snk";
	 var targetDir = "./src/GitVersionExe/bin/" + configuration + "/net40/";
	 var targetPath = targetDir + exeName;

     CreateDirectory(tempMergeDir);
	 string outFilePath = "./" + tempMergeDir + "/" + exeName;	

	 var sourcePattern = targetDir + "*.dll";
	 var sourceFiles = GetFiles(sourcePattern);
	 if(!includeLibGit2Sharp)
	 {
	   	 var excludePattern = "**/LibGit2Sharp.dll";
	     sourceFiles = sourceFiles - GetFiles(excludePattern);	
	 }
	 var settings = new ILRepackSettings { AllowDup = "", Keyfile = keyFilePath, Internalize = true, NDebug = true, TargetKind = TargetKind.Exe, TargetPlatform  = TargetPlatformVersion.v4, XmlDocs = false };
	 ILRepack(outFilePath, targetPath, sourceFiles, settings);   
}


Task("Commandline-Package")
    .IsDependentOn("Build")   
    .Does(() =>
{   

	 ILRepackGitVersionExe(false);  
    
	 var outputDir = buildDir + "NuGetCommandLineBuild";
	 var toolsDir = outputDir + "/tools";
	 var libDir = toolsDir + "/lib";

	 CreateDirectory(outputDir);
	 CreateDirectory(toolsDir);
	 CreateDirectory(libDir);
	 
	 var targetDir = "./src/GitVersionExe/bin/" + configuration + "/net40/";	

	var libGitFiles = GetFiles(targetDir + "LibGit2Sharp.dll*");    
	var nugetAssetsPath = "./src/GitVersionExe/NugetAssets/";	
	Information("Copying files to packaging direcory..");
	
	CopyFiles(targetDir + "GitVersion.pdb", outputDir + "/tools/");
	CopyFiles(targetDir + "GitVersion.exe.mdb", outputDir + "/tools/");

	Information("Copying IL Merged exe file..");
	CopyFiles("./ILMergeTemp/GitVersion.exe", outputDir + "/tools/");

	Information("Copying nuget assets..");	
	CopyFiles(nugetAssetsPath + "GitVersion.CommandLine.nuspec", outputDir);

	Information("Copying libgit2sharp files..");	
	CopyFiles(libGitFiles, outputDir + "/tools/"); 
	CopyDirectory(targetDir + "lib/", outputDir + "/tools/lib/"); 	

	Information("Creating Nuget Package..");	
	var nuGetPackSettings  = new NuGetPackSettings {  Version = nugetVersion, BasePath  = outputDir, OutputDirectory = outputDir };	
	NuGetPack(outputDir + "/GitVersion.CommandLine.nuspec", nuGetPackSettings);			
	
})
.ReportError(exception =>
{  
	Error(exception.Dump());
    // Report the error.
});


Task("Portable-Package")
    .IsDependentOn("Build")
    .Does(() =>
{   

	 ILRepackGitVersionExe(true);  
   
	 var outputDir = buildDir + "NuGetExeBuild";
	 var toolsDir = outputDir + "/tools";
	 var libDir = toolsDir + "/lib";

	 CreateDirectory(outputDir);
	 CreateDirectory(toolsDir);
	 CreateDirectory(libDir);
	 
	 var targetDir = "./src/GitVersionExe/bin/" + configuration + "/net40/";	
	
	var nugetAssetsPath = "./src/GitVersionExe/NugetAssets/";	
	Information("Copying files to packaging direcory..");
	
	CopyFiles(targetDir + "GitVersion.pdb", outputDir + "/tools/");
	CopyFiles(targetDir + "GitVersion.exe.mdb", outputDir + "/tools/");

	Information("Copying IL Merged exe file..");
	CopyFiles("./ILMergeTemp/GitVersion.exe", outputDir + "/tools/");

	Information("Copying nuget assets..");
	CopyFiles(nugetAssetsPath + "*.ps1", outputDir + "/tools/");
	CopyFiles(nugetAssetsPath + "GitVersion.Portable.nuspec", outputDir);

	Information("Copying libgit2sharp files..");		
	CopyDirectory(targetDir + "lib/", outputDir + "/tools/lib/"); 

	var nuGetPackSettings  = new NuGetPackSettings {  Version = nugetVersion, BasePath  = outputDir, OutputDirectory = outputDir };
    NuGetPack(outputDir + "/GitVersion.Portable.nuspec", nuGetPackSettings);

})
.ReportError(exception =>
{  
	Error(exception.Dump());
});


Task("GitVersionCore-Package")
    .IsDependentOn("Build") 
    .Does(() =>
{
     var outputDir = buildDir + "NuGetRefBuild";
	 CreateDirectory(outputDir);

	 var msBuildSettings = new DotNetCoreMSBuildSettings();
	 msBuildSettings.SetVersion(nugetVersion);
     msBuildSettings.Properties["PackageVersion"] = new string[]{ nugetVersion };
     var settings = new DotNetCorePackSettings
     {
         Configuration = configuration,
         OutputDirectory = outputDir,
		 NoBuild = true,
		 MSBuildSettings = msBuildSettings
     };

     DotNetCorePack("./src/GitVersionCore", settings);
})
.ReportError(exception =>
{  
	Error(exception.Dump());    
});

Task("GitVersion-DotNet-Package")
    .IsDependentOn("Build") 
    .Does(() =>
{
   
    // var publishDir = buildDir + "Published";
	// CreateDirectory(outputDir);

	 var outputDir = buildDir + "NuGetExeDotNetCoreBuild"; 
	 var toolsDir = outputDir + "/tools";
	 var libDir = toolsDir + "/lib";

	 CreateDirectory(outputDir);
	 CreateDirectory(toolsDir);
	 CreateDirectory(libDir);


	 var msBuildSettings = new DotNetCoreMSBuildSettings();
	 msBuildSettings.SetVersion(nugetVersion);
     msBuildSettings.Properties["PackageVersion"] = new string[]{ nugetVersion };

	 var framework = "netcoreapp20";

	 var settings = new DotNetCorePublishSettings
     {
         Framework = framework,
         Configuration = configuration,
         OutputDirectory = toolsDir,
		 MSBuildSettings = msBuildSettings
     };

     DotNetCorePublish("./src/GitVersionExe", settings);  	 

	
	 
	 // var targetDir = "./src/GitVersionExe/bin/" + configuration + "/" + framework + "/";	
	
	var nugetAssetsPath = "./src/GitVersionExe/NugetAssets/";	
	Information("Copying files to packaging direcory..");

	Information("Copying nuget assets..");	
	CopyFiles(nugetAssetsPath + "GitVersion.CommandLine.DotNetCore.nuspec", outputDir);

	//Information("Copying libgit2sharp files..");		
	//CopyDirectory(targetDir + "lib/", outputDir + "/tools/lib/"); 

	var nuGetPackSettings  = new NuGetPackSettings {  Version = nugetVersion, BasePath  = outputDir, OutputDirectory = outputDir };
    NuGetPack(outputDir + "/GitVersion.CommandLine.DotNetCore.nuspec", nuGetPackSettings);	
})
.ReportError(exception =>
{  
	Error(exception.Dump());    
});


Task("GitVersionTaskPackage")
    .Description("Produces the nuget package for GitVersionTask")
    .Does(() =>
{

	 var outputDir = buildDir + "NuGetTaskBuild";
	 CreateDirectory(outputDir);

	 var msBuildSettings = new DotNetCoreMSBuildSettings();
	 msBuildSettings.SetVersion(nugetVersion);
	
	 msBuildSettings.Properties["PackageVersion"] = new string[]{ nugetVersion };
     var settings = new DotNetCorePackSettings
     {
         Configuration = configuration,
         OutputDirectory = outputDir,
		 NoBuild = true,
		 MSBuildSettings = msBuildSettings
     };

     DotNetCorePack("./src/GitVersionTask", settings);
	
})
.ReportError(exception =>
{  
	Error(exception.Dump());    
});

Task("Zip-Files")
    .IsDependentOn("Build")
	.IsDependentOn("Commandline-Package")	
	.IsDependentOn("Portable-Package")	
	.IsDependentOn("GitVersionCore-Package")	
	.IsDependentOn("GitVersionTaskPackage")	
	.IsDependentOn("GitVersion-DotNet-Package")		
	.IsDependentOn("Run-Tests-In-NUnitConsole")
    .Does(() =>
{
    Zip("./build/NuGetCommandLineBuild/tools/", "build/GitVersion_" + nugetVersion + ".zip");
	Zip("./build/NuGetExeDotNetCoreBuild/tools/", "build/GitVersionDotNetCore_" + nugetVersion + ".zip");
})
.ReportError(exception =>
{  
	Error(exception.Dump());
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
		var gitReleasNotesExePath = Context.Tools.Resolve("GitReleaseNotes.exe");
        EnsureDirectoryExists(buildDir); 
        var releaseNotesExitCode = StartProcess(
                gitReleasNotesExePath,
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
})
.ReportError(exception =>
{  
	Error(exception.Dump());
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
		"NuGetExeDotNetCoreBuild:GitVersion.CommandLine.DotNetCore." + nugetVersion +".nupkg",
        "NuGetRefBuild:GitVersion." + nugetVersion +".nupkg",
        "NuGetTaskBuild:GitVersionTask." + nugetVersion +".nupkg",   
        "zip:GitVersion_" + nugetVersion + ".zip",
		"zip-dotnetcore:GitVersionDotNetCore_" + nugetVersion + ".zip"
		// "GitVersionTfsTaskBuild:gittools.gitversion-" + semVersion +".vsix",
        // "GemBuild:" + gem
    });

    AppVeyor.UploadArtifact("build/NuGetExeBuild/GitVersion.Portable." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/NuGetCommandLineBuild/GitVersion.CommandLine." + nugetVersion +".nupkg");
	AppVeyor.UploadArtifact("build/NuGetExeDotNetCoreBuild/GitVersion.CommandLine.DotNetCore." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/NuGetRefBuild/GitVersionCore." + nugetVersion +".nupkg");
    AppVeyor.UploadArtifact("build/NuGetTaskBuild/GitVersionTask." + nugetVersion +".nupkg");   
    AppVeyor.UploadArtifact("build/GitVersion_" + nugetVersion + ".zip");
	AppVeyor.UploadArtifact("build/GitVersionDotNetCore_" + nugetVersion + ".zip");
	// AppVeyor.UploadArtifact("build/GitVersionTfsTaskBuild/gittools.gitversion-" + semVersion + ".vsix");
    // AppVeyor.UploadArtifact("build/GemBuild/" + gem);
    AppVeyor.UploadArtifact("build/artifacts");

    if(IsMainGitVersionRepo && IsMainGitVersionBranch && !IsPullRequest)
    {
        if(FileExists("build/releasenotes.md"))
        {
            AppVeyor.UploadArtifact("build/releasenotes.md");
        }
    }
})
.ReportError(exception =>
{  
	Error(exception.Dump());
});

Task("Travis")
  .IsDependentOn("Run-Tests");

Task("Default")
  .IsDependentOn("Upload-AppVeyor-Artifacts");

RunTarget(target);