using Common.Context;
using Docker.Utilities;

namespace Docker;

public class BuildContext(ICakeContext context) : DockerBuildContext(context)
{
    public Credentials? Credentials { get; set; }
}
