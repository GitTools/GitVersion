# YamlDotNet Source Generator

This project uses the YamlDotNet source generator infrastructure (`Vecc.YamlDotNet.Analyzers.StaticGenerator`) to prepare for Native AOT support.

## Current Status

The source generator infrastructure is in place but **not actively used** due to current limitations:

### Why Not Fully Implemented?

1. **Custom Type Inspector Limitation**: The generated `StaticTypeInspector` doesn't properly honor custom type inspectors, which GitVersion requires for `JsonPropertyName` attribute support
2. **Dynamic Type Support**: The source generator doesn't support `Dictionary<object, object?>` used in override configurations
3. **Property Compatibility**: Required changing `internal init` to `internal set` for all configuration properties

### What's Ready?

- `GitVersionConfigurationStaticContext` class with `[YamlStaticContext]` attribute
- `[YamlSerializable]` attributes for main configuration types
- Properties changed from `init` to `set` for generator compatibility
- Source generator package reference and build integration

### Future Use

When YamlDotNet source generator improves support for:
- Custom type inspectors
- Init-only properties
- Complex/dynamic types

We can enable it in `ConfigurationSerializer.cs` by uncommenting the static builder usage.

## References

- [Blog Post: Using YamlDotNet Source Generator for Native AOT](https://andrewlock.net/using-the-yamldotnet-source-generator-for-native-aot/)
- [YamlDotNet Source Generator Package](https://www.nuget.org/packages/Vecc.YamlDotNet.Analyzers.StaticGenerator/)
