namespace GitVersion
{
    using YamlDotNet.Serialization;

    public class BranchConfig
    {
        [YamlAlias("mode")]
        public VersioningMode? VersioningMode { get; set; }

        [YamlAlias("tag")]
        public string Tag { get; set; }

        [YamlAlias("increment")]
        public IncrementStrategy? Increment { get; set; }
    }
}
