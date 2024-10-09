using Common.Utilities;

namespace Artifacts.Tasks;

[TaskName(nameof(ArtifactsDotnetToolTest))]
[TaskDescription("Tests the dotnet global tool in docker container")]
[TaskArgument(Arguments.DockerRegistry, Constants.DockerHub, Constants.GitHub)]
[TaskArgument(Arguments.DockerDotnetVersion, Constants.VersionCurrent, Constants.VersionLatest)]
[TaskArgument(Arguments.DockerDistro, Constants.AlpineLatest, Constants.DebianLatest, Constants.UbuntuLatest)]
[IsDependentOn(typeof(ArtifactsPrepare))]
public class ArtifactsDotnetToolTest : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsDockerOnLinux, $"{nameof(ArtifactsDotnetToolTest)} works only on Docker on Linux agents.");

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        if (context.Version == null)
            return;
        var rootPrefix = string.Empty;
        var version = context.Version.NugetVersion;

        foreach (var dockerImage in context.Images)
        {
            if (context.SkipImageTesting(dockerImage)) continue;

            var cmd = $"{rootPrefix}/scripts/test-global-tool.sh --version {version} --nugetPath {rootPrefix}/nuget --repoPath {rootPrefix}/repo";

            context.DockerTestArtifact(dockerImage, cmd);
        }
    }
}
