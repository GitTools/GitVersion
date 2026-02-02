using Common.Utilities;

namespace Docker.Tasks;

[TaskName(nameof(DockerPublish))]
[TaskDescription("Publish the docker images containing the GitVersion Tool")]
[DockerRegistryArgument]
[DockerDotnetArgument]
[DockerDistroArgument]
[ArchitectureArgument]
[IsDependentOn(typeof(DockerPublishInternal))]
public class DockerPublish : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsGitHubActionsBuild, $"{nameof(DockerPublish)} works only on GitHub Actions.");
        return shouldRun;
    }
}

[TaskName(nameof(DockerPublishInternal))]
[TaskDescription("Publish the docker images containing the GitVersion Tool")]
[IsDependentOn(typeof(DockerTest))]
public class DockerPublishInternal : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsGitHubActionsBuild, $"{nameof(DockerPublish)} works only on GitHub Actions.");
        shouldRun &= context.ShouldRun(context.IsDockerOnLinux, $"{nameof(DockerPublish)} works only on Docker on Linux agents.");
        if (context.DockerRegistry == DockerRegistry.GitHub)
        {
            shouldRun &= context.ShouldRun(context.IsInternalPreRelease, $"{nameof(DockerPublish)} to GitHub Package Registry works only for internal releases.");
        }
        if (context.DockerRegistry == DockerRegistry.DockerHub)
        {
            shouldRun &= context.ShouldRun(context.IsTaggedRelease || context.IsTaggedPreRelease, $"{nameof(DockerPublish)} to DockerHub works only for tagged releases.");
        }

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        foreach (var dockerImage in context.Images)
        {
            context.DockerPushImage(dockerImage);
        }
    }
}
