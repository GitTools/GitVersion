using Common.Context;

namespace Artifacts;

public class BuildContext(ICakeContext context) : DockerBuildContext(context)
{
    public string MsBuildConfiguration { get; } = Constants.DefaultConfiguration;
}
