using Common.Utilities;
using Release.Utilities;

namespace Release;

public class BuildContext(ICakeContext context) : BuildContextBase(context)
{
    public Credentials? Credentials { get; set; }
}
