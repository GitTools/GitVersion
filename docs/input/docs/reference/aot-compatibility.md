# YamlDotNet AOT Compatibility Status

## Summary

This document describes the AOT (Ahead-of-Time) compilation compatibility status of YamlDotNet in the GitVersion project and the infrastructure that has been put in place to support future AOT scenarios.

## Current Status

✅ **Infrastructure Added**: The necessary infrastructure for AOT compatibility has been added to the project.
⚠️ **Source Generator Issues**: The YamlDotNet source generator currently has known bugs that prevent full AOT functionality.
📋 **Ready for Future**: Once the source generator issues are resolved, minimal changes will be needed to enable full AOT support.

## What Has Been Implemented

### 1. YamlDotNet.Analyzers.StaticGenerator Package

The `Vecc.YamlDotNet.Analyzers.StaticGenerator` package (version 16.3.0) has been added to the project. This package provides a source generator that is designed to create AOT-compatible serialization code at compile time, eliminating the need for runtime reflection.

**Package Reference:**
```xml
<PackageReference Include="Vecc.YamlDotNet.Analyzers.StaticGenerator" Version="16.3.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

### 2. YamlConfigurationContext Class

A static context class has been created in the `GitVersion.Configuration` namespace. This class declares all configuration types that need to be serialized/deserialized:

```csharp
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
```

The source generator is designed to create the implementation of this partial class at compile time.

### 3. Tests

Basic tests have been added to verify the infrastructure is in place (`YamlAotCompatibilityTests`).

## Known Issues

### Source Generator Warnings

When building the project, you may see warnings like:

```
warning CS8785: Generator 'TypeFactoryGenerator' failed to generate source. 
It will not contribute to the output and compilation errors may occur as a result. 
Exception was of type 'IndexOutOfRangeException' with message 'Index was outside the bounds of the array.'.
```

**Impact**: The source generator fails to generate the necessary code, so `StaticSerializerBuilder` and `StaticDeserializerBuilder` cannot be used yet.

**Cause**: This is a known issue with the `Vecc.YamlDotNet.Analyzers.StaticGenerator` package when processing certain types, particularly:
- Record types
- Complex nested types
- Types with certain patterns

**Tracking**: See https://github.com/aaubry/YamlDotNet/issues/740 and related issues.

## Current Implementation

The `ConfigurationSerializer` class continues to use the reflection-based `SerializerBuilder` and `DeserializerBuilder` for compatibility:

```csharp
private static IDeserializer Deserializer => new DeserializerBuilder()
    .WithNamingConvention(HyphenatedNamingConvention.Instance)
    .WithTypeConverter(VersionStrategiesConverter.Instance)
    .WithTypeInspector(inspector => new JsonPropertyNameInspector(inspector))
    .Build();
```

This works correctly but relies on reflection, which is not compatible with Native AOT.

## Future Migration Path

When the source generator issues are resolved, the migration to AOT-compatible serialization will involve:

### 1. Update ConfigurationSerializer

Replace the reflection-based builders with static builders:

```csharp
private static YamlConfigurationContext Context => new();

private static IDeserializer Deserializer => new StaticDeserializerBuilder(Context)
    .WithNamingConvention(HyphenatedNamingConvention.Instance)
    .WithTypeConverter(VersionStrategiesConverter.Instance)
    .WithTypeInspector(inspector => new JsonPropertyNameInspector(inspector))
    .Build();

private static ISerializer Serializer => new StaticSerializerBuilder(Context)
    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
    .WithTypeInspector(inspector => new JsonPropertyNameInspector(inspector))
    .WithNamingConvention(HyphenatedNamingConvention.Instance)
    .Build();
```

### 2. Enable AOT in CLI Project

Add AOT publishing configuration to `GitVersion.Cli.csproj`:

```xml
<PropertyGroup>
    <PublishAot>true</PublishAot>
    <IsAotCompatible>true</IsAotCompatible>
</PropertyGroup>
```

### 3. Address Custom Type Converters

Custom type converters like `VersionStrategiesConverter` may need to be reviewed for AOT compatibility. If they use reflection internally, they will need to be updated.

## Alternative Approaches

If the source generator continues to have issues, alternative approaches include:

1. **Manual StaticContext Implementation**: Implement the required methods of `StaticContext` manually without relying on the source generator.

2. **Hybrid Approach**: Use static serialization for simple types and keep reflection-based serialization for complex custom types, with conditional compilation based on AOT mode.

3. **Alternative Serializers**: Consider alternative YAML libraries that have better AOT support, though this would be a significant change.

## Recommendations

1. **Monitor YamlDotNet Updates**: Keep an eye on YamlDotNet releases for source generator improvements.

2. **Test AOT Compatibility**: Once source generator issues are resolved, test the entire application with `PublishAot` to identify any remaining reflection dependencies.

3. **Incremental Migration**: Start with simple configuration types to verify the source generator works before migrating the entire configuration system.

## Compatibility Matrix

| Component | AOT Ready | Notes |
|-----------|-----------|-------|
| YamlDotNet 16.3.0 | ⚠️ Partial | Core library supports AOT, but source generator has issues |
| YamlConfigurationContext | ✅ Yes | Infrastructure in place |
| ConfigurationSerializer | ❌ No | Still uses reflection-based builders |
| VersionStrategiesConverter | ❌ No | Uses reflection in nested serializers |
| CLI Project | ❌ No | No AOT publishing configuration yet |

## References

- [Andrew Lock: Using the YamlDotNet source generator for Native AOT](https://andrewlock.net/using-the-yamldotnet-source-generator-for-native-aot/)
- [YamlDotNet GitHub Issue #740: Support for Native AOT](https://github.com/aaubry/YamlDotNet/issues/740)
- [YamlDotNet GitHub Repository](https://github.com/aaubry/YamlDotNet)
- [Vecc.YamlDotNet.Analyzers.StaticGenerator NuGet Package](https://www.nuget.org/packages/Vecc.YamlDotNet.Analyzers.StaticGenerator)
- [Microsoft: Prepare .NET libraries for trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming)

## Conclusion

The infrastructure for YamlDotNet AOT compatibility has been successfully added to the GitVersion project. While the source generator currently has known issues that prevent immediate use, the groundwork is in place for a smooth transition once these issues are resolved. The project is positioned to take advantage of AOT compilation benefits as soon as the YamlDotNet ecosystem matures.
