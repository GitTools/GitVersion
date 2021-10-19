using Common.Utilities;

namespace Docker;

public class BuildContext : BuildContextBase
{
    public bool IsDockerOnLinux { get; set; }

    public IEnumerable<DockerImage> Images { get; set; } = new List<DockerImage>();
    public DockerRegistry DockerRegistry { get; set; }

    public BuildContext(ICakeContext context) : base(context)
    {
    }
}
