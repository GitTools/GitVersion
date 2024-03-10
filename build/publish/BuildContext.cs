using Common.Utilities;
using Publish.Utilities;

namespace Publish;

public class BuildContext(ICakeContext context) : BuildContextBase(context)
{
    public Credentials? Credentials { get; set; }

    public List<NugetPackage> Packages { get; } = [];
}
