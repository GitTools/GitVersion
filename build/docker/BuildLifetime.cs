using Common.Lifetime;
using Docker.Utilities;

namespace Docker;

public class BuildLifetime : DockerBuildLifetime<BuildContext>
{
    public override void Setup(BuildContext context, ISetupContext info)
    {
        base.Setup(context, info);

        context.Credentials = Credentials.GetCredentials(context);
    }
}
