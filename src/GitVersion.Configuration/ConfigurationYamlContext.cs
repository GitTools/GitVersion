using SharpYaml;
using SharpYaml.Serialization;

namespace GitVersion.Configuration;

[YamlSourceGenerationOptions(
    DefaultIgnoreCondition = YamlIgnoreCondition.WhenWritingNull,
    Converters = [typeof(VersionStrategiesConverter)])]
[YamlSerializable(typeof(GitVersionConfiguration))]
[YamlSerializable(typeof(BranchConfiguration))]
[YamlSerializable(typeof(PreventIncrementConfiguration))]
[YamlSerializable(typeof(IgnoreConfiguration))]
internal partial class ConfigurationYamlContext : YamlSerializerContext;
