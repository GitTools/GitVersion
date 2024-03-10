using Common.Utilities;

namespace Artifacts;

public class BuildContext(ICakeContext context) : BuildContextBase(context)
{
    public string MsBuildConfiguration { get; } = Constants.DefaultConfiguration;

    public bool IsDockerOnLinux { get; set; }

    public IEnumerable<DockerImage> Images { get; set; } = new List<DockerImage>();
}
