using Cake.Core;
using Cake.Frosting;

namespace Chores;

public class BuildContext : FrostingContext
{
    public BuildContext(ICakeContext context) : base(context)
    {
    }
}