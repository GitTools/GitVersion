using Common.Utilities;

namespace Docker.Tasks;

[TaskName(nameof(DockerManifest))]
[TaskDescription("Publish the docker manifest containing the images for amd64 and arm64")]
[TaskArgument(Arguments.DockerRegistry, Constants.DockerHub, Constants.GitHub)]
[TaskArgument(Arguments.DockerDotnetVersion, Constants.Version60, Constants.Version31)]
[TaskArgument(Arguments.DockerDistro, Constants.Alpine312, Constants.Debian10, Constants.Ubuntu2004)]
[IsDependentOn(typeof(DockerManifestInternal))]
public class DockerManifest : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsGitHubActionsBuild, $"{nameof(DockerPublish)} works only on GitHub Actions.");
        return shouldRun;
    }
}

[TaskName(nameof(DockerManifestInternal))]
[TaskDescription("Publish the docker manifest containing the images for amd64 and arm64")]
public class DockerManifestInternal : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsGitHubActionsBuild, $"{nameof(DockerPublish)} works only on GitHub Actions.");
        shouldRun &= context.ShouldRun(context.IsDockerOnLinux, $"{nameof(DockerPublish)} works only on Docker on Linux agents.");

        if (context.DockerRegistry == DockerRegistry.GitHub)
        {
            shouldRun &= context.ShouldRun(context.IsStableRelease || context.IsPreRelease, $"{nameof(DockerPublish)} to GitHub Package Registry works only for releases.");
        }
        if (context.DockerRegistry == DockerRegistry.DockerHub)
        {
            shouldRun &= context.ShouldRun(context.IsStableRelease, $"{nameof(DockerPublish)} DockerHub works only for tagged releases.");
        }

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        foreach (var group in context.Images.GroupBy(x => new { x.Distro, x.TargetFramework }))
        {
            var amd64DockerImage = group.First(x => x.Architecture == Architecture.Amd64);
            var arm64DockerImage = group.First(x => x.Architecture == Architecture.Arm64);
            context.DockerCreateManifest(amd64DockerImage, context.SkipArm64Image(arm64DockerImage));
            context.DockerPushManifest(amd64DockerImage);
        }
    }
}
