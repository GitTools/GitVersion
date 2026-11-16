using Common.Lifetime;
using Common.Utilities;
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
            context.DockerRegistry = context.Argument(Arguments.DockerRegistry, DockerRegistry.DockerHub);
        }

        base.Setup(context, info);

        context.Credentials = Credentials.GetCredentials(context);
    }
}
