using Common.Utilities;
using Release.Utilities;

namespace Release;

public class BuildContext : BuildContextBase
{
    public Credentials? Credentials { get; set; }

    public BuildContext(ICakeContext context) : base(context)
    {
    }
}
