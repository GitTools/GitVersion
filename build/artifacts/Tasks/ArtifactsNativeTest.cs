using Cake.Frosting;
using Common.Utilities;

namespace Artifacts.Tasks
{
    [TaskName(nameof(ArtifactsNativeTest))]
    [TaskDescription("Tests the native executables in docker container")]
    [TaskArgument(Arguments.DockerRegistry, Constants.GitHub, Constants.DockerHub)]
    [TaskArgument(Arguments.DockerDotnetVersion, Constants.Version50, Constants.Version31)]
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
            if (context.Version == null)
                return;
            var version = context.Version.SemVersion;
            var rootPrefix = string.Empty;

            foreach (var dockerImage in context.Images)
            {
                var runtime = "linux-x64";
                if (dockerImage.Distro.StartsWith("alpine"))
                {
                    runtime = "linux-musl-x64";
                }

                var cmd = $"-file {rootPrefix}/scripts/Test-Native.ps1 -version {version} -repoPath {rootPrefix}/repo -runtime {runtime}";

                context.DockerTestArtifact(dockerImage, cmd);
            }
        }
    }
}
