using Common.Lifetime;
using Docker.Tasks;
using Docker.Utilities;

namespace Docker;

public class BuildLifetime : DockerBuildLifetime<BuildContext>
{
    public override void Setup(BuildContext context, ISetupContext info)
    {
        if (info.TargetTask.Name is nameof(DockerHubReadmePublish) or nameof(DockerHubReadmePublishInternal))
        {
            // README publication uses the Docker Hub HTTP API, not the Docker daemon.
            context.UseDocker = false;
        }

        base.Setup(context, info);

        context.Credentials = Credentials.GetCredentials(context);
    }
}
