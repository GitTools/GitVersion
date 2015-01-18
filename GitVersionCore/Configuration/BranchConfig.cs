namespace GitVersion
{
    using YamlDotNet.Serialization;

    public class BranchConfig
    {
        public BranchConfig()
        {
        }

        public BranchConfig(BranchConfig branchConfiguration)
        {
            VersioningMode = branchConfiguration.VersioningMode;
            Tag = branchConfiguration.Tag;
            Increment = branchConfiguration.Increment;
        }

        [YamlMember(Alias = "mode")]
        public VersioningMode? VersioningMode { get; set; }

        /// <summary>
        /// Special value 'useBranchName' will extract the tag from the branch name
        /// </summary>
        [YamlMember(Alias = "tag")]
        public string Tag { get; set; }

        [YamlMember(Alias = "increment")]
        public IncrementStrategy? Increment { get; set; }
    }
}
