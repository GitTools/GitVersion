using GitVersion.Configuration;
namespace GitVersion
{
    using System.Collections;
    using System.Collections.Generic;

    using YamlDotNet.Serialization;

    public class BranchConfig
    {
        public BranchConfig()
        {
            Prefixes = new List<string>();
        }

        [YamlAlias("mode")]
        public VersioningMode VersioningMode { get; set; }

        [YamlAlias("tag")]
        public string Tag { get; set; }

        public IncrementType IncrementType { get; set; }

        public string ReferenceBranch { get; set; }

        public IEnumerable<string> Prefixes { get; set; }

        public string LastTagReferenceBranch { get; set; }

        public bool IncrementOnTag { get; set; }

        public bool ForceBuildMetdataFromReferenceBranch { get; set; }
    }
}
