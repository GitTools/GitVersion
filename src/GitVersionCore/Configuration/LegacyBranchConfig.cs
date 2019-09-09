using YamlDotNet.Serialization;

namespace GitVersion.Configuration
{
    /// <summary>
    /// Obsolete properties are added to this, so we can check to see if they are used and provide good error messages for migration
    /// </summary>
    public class LegacyBranchConfig
    {
        [YamlMember(Alias = "is-develop")]
        public string IsDevelop
        {
            get; set;
        }
    }
}
