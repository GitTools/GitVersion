using Common.Utilities;
using Publish.Utilities;

namespace Publish;

public class BuildContext : BuildContextBase
{
    public Credentials? Credentials { get; set; }

    public List<NugetPackage> Packages { get; set; } = new();
    public BuildContext(ICakeContext context) : base(context)
    {
    }
}
