using Common.Utilities;

namespace Artifacts.Tasks;

[TaskName(nameof(ArtifactsMsBuildFullTest))]
[TaskDescription("Tests the MSBuild package with a file-based app on Windows")]
public class ArtifactsMsBuildFullTest : FrostingTask<BuildContext>
{
    /// <summary>
    /// Restricts this artifact smoke test to Windows agents.
    /// </summary>
    public override bool ShouldRun(BuildContext context)
        => context.ShouldRun(context.IsOnWindows, $"{nameof(ArtifactsMsBuildFullTest)} works only on Windows agents.");

    /// <summary>
    /// Runs the file-based consumer for each target framework and verifies its generated full semantic version.
    /// </summary>
    public override void Run(BuildContext context)
    {
        if (context.Version == null)
        {
            return;
        }

        var source = context.MakeAbsolute(Paths.Integration.CombineWithFilePath("Program.cs"));
        var nugetSource = context.MakeAbsolute(Paths.Nuget);
        foreach (var netVersion in Constants.DotnetVersions)
        {
            var arguments = new ProcessArgumentBuilder()
                .Append("run --file")
                .AppendQuoted(source.FullPath)
                .Append("--no-launch-profile --verbosity quiet --configuration")
                .AppendQuoted(context.MsBuildConfiguration)
                .AppendQuoted($"-p:GitVersionMsBuildVersion={context.Version.NugetVersion}")
                .AppendQuoted($"-p:TargetFramework=net{netVersion}")
                .AppendQuoted($"-p:RestoreAdditionalProjectSources={nugetSource.FullPath}");
            var exitCode = context.StartProcess("dotnet", new ProcessSettings
            {
                Arguments = arguments,
                RedirectStandardOutput = true
            }, out var output);
            var actual = string.Concat(output);
            context.Information(actual);
            if (exitCode != 0 || actual != context.Version.GitVersion.FullSemVer)
            {
                throw new InvalidOperationException($"MSBuild artifact smoke test failed: exit code {exitCode}, expected '{context.Version.GitVersion.FullSemVer}', got '{actual}'.");
            }
        }
    }
}
