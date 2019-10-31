using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace GitVersion.Configuration
{
    /// <summary>
    /// Obsolete properties are added to this, so we can check to see if they are used and provide good error messages for migration
    /// </summary>
    public class LegacyConfig
    {
        private readonly Dictionary<string, LegacyBranchConfig> branches = new Dictionary<string, LegacyBranchConfig>();

        [YamlMember(Alias = "assemblyVersioningScheme")]
        public string AssemblyVersioningScheme { get; set; }

        [YamlMember(Alias = "develop-branch-tag")]
        public string DevelopBranchTag { get; set; }

        [YamlMember(Alias = "release-branch-tag")]
        public string ReleaseBranchTag { get; set; }

        [YamlMember(Alias = "branches")]
        public Dictionary<string, LegacyBranchConfig> Branches
        {
            get => branches;
            set
            {
                value.ToList().ForEach(_ =>
                {
                    if (!branches.ContainsKey(_.Key))
                        branches.Add(_.Key, new LegacyBranchConfig());

                    branches[_.Key] = MergeObjects(branches[_.Key], _.Value);
                });
            }
        }

        private T MergeObjects<T>(T target, T source)
        {
            
            var typeInfo = typeof(T);
            typeInfo.GetProperties()
                .Where(prop => prop.CanRead && prop.CanWrite)
                .Select(_ => new
                {
                    prop = _,
                    value = _.GetValue(source, null)
                })
                .Where(_ => _.value != null)
                .ToList()
                .ForEach(_ => _.prop.SetValue(target, _.value, null));
            return target;
        }
    }
}
