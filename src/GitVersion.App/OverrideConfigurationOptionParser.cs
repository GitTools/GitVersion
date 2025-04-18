using GitVersion.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion;

internal class OverrideConfigurationOptionParser
{
    private readonly Dictionary<object, object?> overrideConfiguration = [];

    private static readonly Lazy<ILookup<string?, PropertyInfo>> _lazySupportedProperties =
        new(GetSupportedProperties, true);

    internal static ILookup<string?, PropertyInfo> SupportedProperties => _lazySupportedProperties.Value;

    /// <summary>
    /// Dynamically creates <see cref="System.Linq.ILookup{TKey, TElement}"/> of
    /// <see cref="GitVersionConfiguration"/> properties supported as a part of command line '/overrideconfig' option.
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// Lookup keys are created from <see cref="System.Text.Json.Serialization.JsonPropertyNameAttribute"/> to match 'GitVersion.yml', 'GitVersion.yaml', '.GitVersion.yml' or '.GitVersion.yaml' file
    /// options as close as possible.
    /// </remarks>
    private static ILookup<string?, PropertyInfo> GetSupportedProperties() => typeof(GitVersionConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(
            pi => IsSupportedPropertyType(pi.PropertyType)
                  && pi.CanWrite
                  && pi.GetCustomAttributes(typeof(JsonPropertyNameAttribute), false).Length > 0
        )
        .ToLookup(
            pi => (pi.GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)[0] as JsonPropertyNameAttribute)?.Name,
            pi => pi
        );

    /// <summary>
    /// Checks if property <see cref="Type"/> of <see cref="GitVersionConfiguration"/>
    /// is supported as a part of command line '/overrideconfig' option.
    /// </summary>
    /// <param name="propertyType">Type we want to check.</param>
    /// <returns>True, if type is supported.</returns>
    /// <remarks>Only simple types are supported</remarks>
    private static bool IsSupportedPropertyType(Type propertyType)
    {
        var unwrappedType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        return unwrappedType == typeof(string)
               || unwrappedType.IsEnum
               || unwrappedType == typeof(int)
               || unwrappedType == typeof(bool)
               || unwrappedType == typeof(VersionStrategies[]);
    }

    internal void SetValue(string key, string value) => overrideConfiguration[key] = QuotedStringHelpers.UnquoteText(value);

    internal IReadOnlyDictionary<object, object?> GetOverrideConfiguration() => this.overrideConfiguration;
}
