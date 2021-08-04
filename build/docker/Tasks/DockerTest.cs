using Cake.Frosting;
using Common.Utilities;

namespace Docker.Tasks
{
    [TaskName(nameof(DockerTest))]
    [TaskDescription("Test the docker images containing the GitVersion Tool")]
    [IsDependentOn(typeof(DockerBuild))]
    public class DockerTest : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
        {
            var shouldRun = true;
            shouldRun &= context.ShouldRun(context.IsDockerOnLinux, $"{nameof(DockerTest)} works only on Docker on Linux agents.");

            return shouldRun;
        }

        public override void Run(BuildContext context)
        {
            var settings = context.GetDockerRunSettings();

            foreach (var dockerImage in context.Images)
            {
                var tags = context.GetDockerTagsForRepository(dockerImage, context.DockerRegistry);
                foreach (var tag in tags)
                {
                    context.DockerTestRun(settings, tag, "/repo", "/showvariable", "FullSemver");
                }
            }
        }
    }
}
