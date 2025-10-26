using Common.Lifetime;

namespace Artifacts;

public class BuildLifetime : DockerBuildLifetime<BuildContext>
{
    protected override bool UseBaseImage => true;
}
