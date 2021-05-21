using Artifacts.Utilities;
using Cake.Frosting;
using Common.Utilities;

namespace Artifacts.Tasks
{
    [TaskName(nameof(ArtifactsNativeTest))]
    [TaskDescription("Tests the native executables in docker container")]
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
            var rootPrefix = string.Empty;

            foreach (var dockerImage in context.Images)
            {
                var (distro, _) = dockerImage;

                var runtime = "linux-x64";
                if (distro.StartsWith("alpine"))
                {
                    runtime = "linux-musl-x64";
                }

                var cmd = $"-file {rootPrefix}/scripts/Test-Native.ps1 -repoPath {rootPrefix}/repo -runtime {runtime}";

                context.DockerTestArtifact(dockerImage, cmd, Constants.GitHubContainerRegistry);
            }
        }
    }
}
