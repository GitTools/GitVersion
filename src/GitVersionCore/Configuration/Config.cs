namespace GitVersion
{
    using System.Collections.Generic;
    using System.Linq;
    using YamlDotNet.Serialization;

    public class Config
    {
        Dictionary<string, BranchConfig> branches = new Dictionary<string, BranchConfig>();

        [YamlMember(Alias = "assembly-versioning-scheme")]
        public AssemblyVersioningScheme? AssemblyVersioningScheme { get; set; }

        [YamlMember(Alias = "mode")]
        public VersioningMode? VersioningMode { get; set; }

        [YamlMember(Alias = "tag-prefix")]
        public string TagPrefix { get; set; }

        [YamlMember(Alias = "continuous-delivery-fallback-tag")]
        public string ContinuousDeploymentFallbackTag { get; set; }

        [YamlMember(Alias = "next-version")]
        public string NextVersion { get; set; }

        [YamlMember(Alias = "branches")]
        public Dictionary<string, BranchConfig> Branches
        {
            get
            {
                return branches;
            }
            set
            {
                value.ToList().ForEach(_ =>
                {
                    if (!branches.ContainsKey(_.Key))
                        branches.Add(_.Key, new BranchConfig());

                    branches[_.Key] = MergeObjects(branches[_.Key], _.Value);
                });
            }
        }

        private T MergeObjects<T>(T target, T source)
        {
            typeof(T).GetProperties()
                .Where(prop => prop.CanRead && prop.CanWrite)
                .Select(_ => new {prop = _, value =_.GetValue(source, null) } )
                .Where(_ => _.value != null)
                .ToList()
                .ForEach(_ => _.prop.SetValue(target, _.value, null));
            return target;
        }
    }
}