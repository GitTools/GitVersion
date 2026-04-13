using Common.Utilities;

namespace Common.Context;

public class DockerBuildContext(ICakeContext context) : BuildContextBase(context)
{
    public bool IsDockerOnLinux { get; set; }

    public IEnumerable<DockerImage> Images { get; set; } = [];
    public DockerRegistry DockerRegistry { get; set; }
    public ICollection<Architecture> Architectures { get; set; } = [];
}

