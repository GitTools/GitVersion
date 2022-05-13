using Common.Utilities;

namespace Artifacts.Tasks;

[TaskName(nameof(ArtifactsPrepare))]
[TaskDescription("Pulls the docker images needed for testing the artifacts")]
[TaskArgument(Arguments.DockerRegistry, Constants.DockerHub, Constants.GitHub)]
[TaskArgument(Arguments.DockerDotnetVersion, Constants.Version60, Constants.Version31)]
[TaskArgument(Arguments.DockerDistro, Constants.Alpine312, Constants.Debian10, Constants.Ubuntu2004)]
public class ArtifactsPrepare : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsDockerOnLinux, $"{nameof(ArtifactsPrepare)} works only on Docker on Linux agents.");

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        foreach (var dockerImage in context.Images)
        {
            if (context.SkipArm64Image(dockerImage)) continue;
            context.DockerPullImage(dockerImage);
        }
    }
}
