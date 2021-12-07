using Common.Utilities;

namespace Artifacts;

public class BuildContext : BuildContextBase
{
    public string MsBuildConfiguration { get; set; } = "Release";

    public bool IsDockerOnLinux { get; set; }

    public IEnumerable<DockerImage> Images { get; set; } = new List<DockerImage>();

    public BuildContext(ICakeContext context) : base(context)
    {
    }
}
