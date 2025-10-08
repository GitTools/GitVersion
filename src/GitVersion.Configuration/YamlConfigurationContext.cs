using YamlDotNet.Serialization;

namespace GitVersion.Configuration;

/// <summary>
/// Static context for YamlDotNet serialization/deserialization to support AOT compilation.
/// This class is used by the YamlDotNet source generator to generate AOT-compatible serialization code.
/// </summary>
[YamlStaticContext]
[YamlSerializable(typeof(GitVersionConfiguration))]
[YamlSerializable(typeof(BranchConfiguration))]
[YamlSerializable(typeof(IgnoreConfiguration))]
[YamlSerializable(typeof(PreventIncrementConfiguration))]
[YamlSerializable(typeof(Dictionary<string, string>))]
[YamlSerializable(typeof(Dictionary<string, BranchConfiguration>))]
[YamlSerializable(typeof(HashSet<string>))]
public partial class YamlConfigurationContext : StaticContext
{
}
