using Common.Utilities;
using Docker.Utilities;

namespace Docker;

public class BuildContext : BuildContextBase
{
    public Credentials? Credentials { get; set; }
    public bool IsDockerOnLinux { get; set; }

    public IEnumerable<DockerImage> Images { get; set; } = new List<DockerImage>();
    public DockerRegistry DockerRegistry { get; set; }

    public BuildContext(ICakeContext context) : base(context)
    {
    }
}
