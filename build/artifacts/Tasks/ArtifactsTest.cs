using Cake.Frosting;
using Common.Utilities;

namespace Artifacts.Tasks
{
    [TaskName(nameof(ArtifactsMsBuildCoreTest))]
    [TaskDescription("Tests packages in docker container")]
    [IsDependentOn(typeof(ArtifactsDotnetToolTest))]
    [IsDependentOn(typeof(ArtifactsMsBuildCoreTest))]
    public class ArtifactsTest : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
        {
            var shouldRun = true;
            shouldRun &= context.ShouldRun(context.IsDockerOnLinux, "ArtifactsTest works only on Docker on Linux agents.");

            return shouldRun;
        }
    }
}
