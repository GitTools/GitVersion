using System.Collections.Generic;
using Cake.Core;
using Cake.Wyam;
using Common.Utilities;
using Docs.Utilities;

namespace Docs
{
    public class BuildContext : BuildContextBase
    {
        public Credentials? Credentials { get; set; }

        public WyamSettings? WyamSettings { get; set; }

        public BuildContext(ICakeContext context) : base(context)
        {
        }
    }
}
