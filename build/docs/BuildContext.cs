using Cake.Wyam;
using Common.Utilities;
using Docs.Utilities;

namespace Docs;

public class BuildContext(ICakeContext context) : BuildContextBase(context)
{
    public bool ForcePublish { get; set; }
    public Credentials? Credentials { get; set; }
    public WyamSettings? WyamSettings { get; set; }
}
