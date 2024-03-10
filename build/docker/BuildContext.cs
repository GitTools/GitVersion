using Common.Utilities;
using Docker.Utilities;

namespace Docker;

public class BuildContext(ICakeContext context) : BuildContextBase(context)
{
    public Credentials? Credentials { get; set; }
    public bool IsDockerOnLinux { get; set; }

    public IEnumerable<DockerImage> Images { get; set; } = new List<DockerImage>();
    public DockerRegistry DockerRegistry { get; set; }
    public ICollection<Architecture> Architectures { get; set; } = new List<Architecture>();
}
