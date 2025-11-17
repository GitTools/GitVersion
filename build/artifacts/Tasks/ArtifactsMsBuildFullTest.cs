using Common.Utilities;

namespace Artifacts.Tasks;

[TaskName(nameof(ArtifactsMsBuildFullTest))]
[TaskDescription("Tests the msbuild package on windows")]
public class ArtifactsMsBuildFullTest : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsOnWindows, $"{nameof(ArtifactsMsBuildFullTest)} works only on windows agents.");

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        if (context.Version == null)
            return;
        var version = context.Version.NugetVersion;
        var fullSemVer = context.Version.GitVersion.FullSemVer;

        var nugetSource = context.MakeAbsolute(Paths.Nuget).FullPath;

        const int toolVersionValue = 11; // Workaround for now. It should be removed when https://github.com/cake-build/cake/issues/4658 is merged
        var isMsBuildToolVersionValid = Enum.IsDefined(typeof(MSBuildToolVersion), toolVersionValue);

        context.Information("\nTesting msbuild task with dotnet build\n");
        foreach (var netVersion in Constants.DotnetVersions)
        {
            var framework = $"net{netVersion}";
            var dotnetMsBuildSettings = new DotNetMSBuildSettings();
            dotnetMsBuildSettings.SetTargetFramework(framework);
            dotnetMsBuildSettings.WithProperty("GitVersionMsBuildVersion", version);
            var projPath = context.MakeAbsolute(Paths.Integration);

            context.DotNetBuild(projPath.FullPath, new DotNetBuildSettings
            {
                Verbosity = DotNetVerbosity.Minimal,
                Configuration = context.MsBuildConfiguration,
                MSBuildSettings = dotnetMsBuildSettings,
                Sources = [nugetSource]
            });

            var exe = Paths.Integration.Combine("build").Combine(framework).CombineWithFilePath("app.dll");
            context.ValidateOutput("dotnet", exe.FullPath, fullSemVer);

            if (!isMsBuildToolVersionValid) continue;

            const MSBuildToolVersion toolVersion = (MSBuildToolVersion)toolVersionValue;
            context.Information("\nTesting msbuild task with msbuild (for full framework)\n");

            var msBuildSettings = new MSBuildSettings
            {
                Verbosity = Verbosity.Minimal,
                ToolVersion = toolVersion,
                Restore = true
            };

            msBuildSettings.WithProperty("GitVersionMsBuildVersion", version);
            msBuildSettings.WithProperty("RestoreSource", nugetSource);

            context.MSBuild(projPath.FullPath, msBuildSettings);

            var fullExe = Paths.Integration.Combine("build").CombineWithFilePath("app.exe");
            context.ValidateOutput(fullExe.FullPath, null, fullSemVer);
        }
    }
}
