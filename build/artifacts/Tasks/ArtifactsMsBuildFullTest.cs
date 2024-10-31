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
            context.ValidateOutput("dotnet", exe.FullPath, context.Version.GitVersion.FullSemVer);
        }
    }
}
