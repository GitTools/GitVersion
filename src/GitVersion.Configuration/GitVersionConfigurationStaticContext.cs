using YamlDotNet.Serialization;

namespace GitVersion.Configuration;

[YamlStaticContext]
[YamlSerializable(typeof(GitVersionConfiguration))]
[YamlSerializable(typeof(BranchConfiguration))]
[YamlSerializable(typeof(IgnoreConfiguration))]
[YamlSerializable(typeof(PreventIncrementConfiguration))]
public partial class GitVersionConfigurationStaticContext
{
}
