using Common.Utilities;

namespace Docker.Tasks;

[TaskName(nameof(DockerTest))]
[TaskDescription("Test the docker images containing the GitVersion Tool")]
[DockerRegistryArgument]
[DockerDotnetArgument]
[DockerDistroArgument]
[ArchitectureArgument]
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
        foreach (var dockerImage in context.Images)
        {
            if (context.SkipImageTesting(dockerImage)) continue;
            context.DockerTestImage(dockerImage);
        }
    }
}
