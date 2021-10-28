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

        var nugetSource = context.MakeAbsolute(Paths.Nuget).FullPath;

        context.Information("\nTesting msbuild task with dotnet build (for .net core)\n");
        var frameworks = new[] { Constants.CoreFxVersion31, Constants.NetVersion50, Constants.NetVersion60 };
        foreach (var framework in frameworks)
        {
            var dotnetCoreMsBuildSettings = new DotNetCoreMSBuildSettings();
            dotnetCoreMsBuildSettings.WithProperty("TargetFrameworks", framework);
            dotnetCoreMsBuildSettings.WithProperty("TargetFramework", framework);
            dotnetCoreMsBuildSettings.WithProperty("GitVersionMsBuildVersion", version);
            var projPath = context.MakeAbsolute(Paths.Integration.Combine("core"));

            context.DotNetCoreBuild(projPath.FullPath, new DotNetCoreBuildSettings
            {
                Verbosity = DotNetCoreVerbosity.Minimal,
                Configuration = context.MsBuildConfiguration,
                MSBuildSettings = dotnetCoreMsBuildSettings,
                Sources = new[] { nugetSource }
            });

            var netcoreExe = Paths.Integration.Combine("core").Combine("build").Combine(framework).CombineWithFilePath("app.dll");
            context.ValidateOutput("dotnet", netcoreExe.FullPath, context.Version.GitVersion.FullSemVer);
        }

        context.Information("\nTesting msbuild task with msbuild (for full framework)\n");

        var msBuildSettings = new MSBuildSettings
        {
            Verbosity = Verbosity.Minimal,
            Restore = true
        };

        msBuildSettings.WithProperty("GitVersionMsBuildVersion", version);
        msBuildSettings.WithProperty("RestoreSource", nugetSource);

        context.MSBuild("./tests/integration/full", msBuildSettings);

        var fullExe = Paths.Integration.Combine("full").Combine("build").CombineWithFilePath("app.exe");
        context.ValidateOutput(fullExe.FullPath, null, context.Version.GitVersion.FullSemVer);
    }
}
