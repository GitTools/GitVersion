using Artifacts.Tasks;
using Common.Lifetime;
using Common.Utilities;

namespace Artifacts;

public class BuildLifetime : DockerBuildLifetime<BuildContext>
{
    protected override bool UseBaseImage => true;

    public override void Setup(BuildContext context, ISetupContext info)
    {
        var target = context.Argument("target", "Default");
        if (target.IsEqualInvariant(nameof(ArtifactsMsBuildFullTest)) || target.IsEqualInvariant(nameof(ArtifactsExecutableTest)))
        {
            context.UseDocker = false;
        }

        base.Setup(context, info);
    }
}
