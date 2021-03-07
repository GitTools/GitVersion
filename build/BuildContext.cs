using Cake.Core;
using Cake.Frosting;

namespace GitVersion.Build
{
    public class BuildContext : FrostingContext
    {
        public bool Delay { get; set; }

        public BuildContext(ICakeContext context)
            : base(context)
        {
            Delay = context.Arguments.HasArgument("delay");
        }
    }
}
