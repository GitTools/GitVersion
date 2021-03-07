using Cake.Core;
using Cake.Frosting;

namespace GitVersion.Build
{
    public class BuildContext : FrostingContext
    {
        public new string Configuration { get; }
        public Paths Paths { get; set; } = new();
        public BuildContext(ICakeContext context)
            : base(context)
        {
            Configuration = context.Arguments.GetArgument("configuration");
        }
    }

    public class Paths
    {
        public string Artifacts { get; } = "./artifacts";
        public string Src { get; } = "./src";
        public string Build { get; } = "./build";
    }
}
