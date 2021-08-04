using Cake.Core;
using Common.Utilities;
using Publish.Utilities;

namespace Publish
{
    public class BuildContext : BuildContextBase
    {
        public BuildCredentials? Credentials { get; set; }

        public BuildContext(ICakeContext context) : base(context)
        {
        }
    }
}
