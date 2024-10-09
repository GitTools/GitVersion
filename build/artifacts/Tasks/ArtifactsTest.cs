using Common.Utilities;

namespace Artifacts.Tasks;

[TaskName(nameof(ArtifactsTest))]
[TaskDescription("Tests packages in docker container")]
[DockerRegistryArgument]
[DockerDotnetArgument]
[DockerDistroArgument]
[IsDependentOn(typeof(ArtifactsNativeTest))]
[IsDependentOn(typeof(ArtifactsDotnetToolTest))]
[IsDependentOn(typeof(ArtifactsMsBuildCoreTest))]
public class ArtifactsTest : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsDockerOnLinux, $"{nameof(ArtifactsTest)} works only on Docker on Linux agents.");

        return shouldRun;
    }
}
