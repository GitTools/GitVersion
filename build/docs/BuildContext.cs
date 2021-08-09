using System.Collections.Generic;
using Cake.Core;
using Common.Utilities;
using Docs.Utilities;

namespace Docs
{
    public class BuildContext : BuildContextBase
    {
        public Credentials? Credentials { get; set; }

        public Dictionary<string, object> WyamAdditionalSettings = new();

        public BuildContext(ICakeContext context) : base(context)
        {
        }
    }
}
