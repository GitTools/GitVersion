using Common.Utilities;

namespace Artifacts.Tasks;

[TaskName(nameof(ArtifactsNativeTest))]
[TaskDescription("Tests the native executables in docker container")]
[TaskArgument(Arguments.DockerRegistry, Constants.DockerHub, Constants.GitHub)]
[TaskArgument(Arguments.DockerDotnetVersion, Constants.Version60, Constants.Version31)]
[TaskArgument(Arguments.DockerDistro, Constants.Alpine312, Constants.Debian10, Constants.Ubuntu2004)]
[IsDependentOn(typeof(ArtifactsPrepare))]
public class ArtifactsNativeTest : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsDockerOnLinux, $"{nameof(ArtifactsNativeTest)} works only on Docker on Linux agents.");

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        if (context.Version?.SemVersion == null)
            return;
        var version = context.Version.SemVersion.ToLower();
        var rootPrefix = string.Empty;

        foreach (var dockerImage in context.Images)
        {
            if (context.SkipArm64Image(dockerImage)) continue;

            var runtime = dockerImage.Architecture == Architecture.Amd64 ? "linux-x64" : "linux-arm64";
            if (dockerImage.Distro.StartsWith("alpine"))
            {
                runtime = "linux-musl-x64";
            }

            var cmd = $"{rootPrefix}/scripts/test-native-tool.sh --version {version} --repoPath {rootPrefix}/repo --runtime {runtime}";

            context.DockerTestArtifact(dockerImage, cmd);
        }
    }
}
