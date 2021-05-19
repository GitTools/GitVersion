using Artifacts.Utilities;
using Cake.Frosting;
using Common.Utilities;
using Constants = Common.Utilities.Constants;

namespace Artifacts.Tasks
{
    [TaskName(nameof(ArtifactsPrepare))]
    [TaskDescription("Pulls the docker images needed for testing the artifacts")]
    public class ArtifactsPrepare : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
        {
            var shouldRun = true;
            shouldRun &= context.ShouldRun(context.IsDockerOnLinux, "ArtifactsPrepare works only on Docker on Linux agents.");

            return shouldRun;
        }

        public override void Run(BuildContext context)
        {
            foreach (var dockerImage in context.Images)
            {
                context.DockerPullImage(dockerImage, Constants.GitHubContainerRegistry);
            }
        }
    }
}
