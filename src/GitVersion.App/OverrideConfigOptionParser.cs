using GitVersion.Model.Configuration;

namespace GitVersion;

internal class OverrideConfigOptionParser
{
    private static readonly Lazy<ILookup<string, PropertyInfo>> _lazySupportedProperties =
        new(GetSupportedProperties, true);

    private readonly Lazy<Config> lazyConfig = new();

    internal static ILookup<string, PropertyInfo> SupportedProperties => _lazySupportedProperties.Value;

    /// <summary>
    /// Dynamically creates <see cref="System.Linq.ILookup{TKey, TElement}"/> of
    /// <see cref="Config"/> properties supported as a part of command line '/overrideconfig' option.
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// Lookup keys are created from <see cref="YamlDotNet.Serialization.YamlMemberAttribute"/> to match 'GitVersion.yml'
    /// options as close as possible.
    /// </remarks>
    private static ILookup<string, PropertyInfo> GetSupportedProperties() => typeof(Config).GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(
            pi => IsSupportedPropertyType(pi.PropertyType)
                  && pi.CanWrite
                  && pi.GetCustomAttributes(typeof(YamlDotNet.Serialization.YamlMemberAttribute), false).Length > 0
        )
        .ToLookup(
            pi => (pi.GetCustomAttributes(typeof(YamlDotNet.Serialization.YamlMemberAttribute), false)[0] as YamlDotNet.Serialization.YamlMemberAttribute).Alias,
            pi => pi
        );

    /// <summary>
    /// Checks if property <see cref="Type"/> of <see cref="Config"/>
    /// is supported as a part of command line '/overrideconfig' option.
    /// </summary>
    /// <param name="propertyType">Type we want to check.</param>
    /// <returns>True, if type is supported.</returns>
    /// <remarks>Only simple types are supported</remarks>
    private static bool IsSupportedPropertyType(Type propertyType)
    {
        Type unwrappedType = Nullable.GetUnderlyingType(propertyType);
        if (unwrappedType == null)
            unwrappedType = propertyType;

        return unwrappedType == typeof(string)
               || unwrappedType.IsEnum
               || unwrappedType == typeof(int)
               || unwrappedType == typeof(bool);
    }

    internal void SetValue(string key, string value)
    {
        if (!SupportedProperties.Contains(key))
            return;

        var unwrappedText = QuotedStringHelpers.UnquoteText(value);
        foreach (var pi in SupportedProperties[key])
        {
            Type unwrapped = Nullable.GetUnderlyingType(pi.PropertyType);
            if (unwrapped == null)
                unwrapped = pi.PropertyType;

            if (unwrapped == typeof(string))
                pi.SetValue(this.lazyConfig.Value, unwrappedText);
            else if (unwrapped.IsEnum)
            {
                try
                {
                    var parsedEnum = Enum.Parse(unwrapped, unwrappedText);
                    pi.SetValue(this.lazyConfig.Value, parsedEnum);
                }
                catch (ArgumentException)
                {
                    var sb = new StringBuilder();

                    sb.Append($"Could not parse /overrideconfig option: {key}={value}.");
                    sb.AppendLine(" Ensure that 'value' is valid for specified 'key' enumeration: ");
                    foreach (var name in Enum.GetNames(unwrapped))
                        sb.AppendLine(name);

                    throw new WarningException(sb.ToString());
                }
            }
            else if (unwrapped == typeof(int))
            {
                if (int.TryParse(unwrappedText, out int parsedInt))
                    pi.SetValue(this.lazyConfig.Value, parsedInt);
                else
                    throw new WarningException($"Could not parse /overrideconfig option: {key}={value}. Ensure that 'value' is valid integer number.");
            }
            else if (unwrapped == typeof(bool))
            {
                if (bool.TryParse(unwrappedText, out bool parsedBool))
                    pi.SetValue(this.lazyConfig.Value, parsedBool);
                else
                    throw new WarningException($"Could not parse /overrideconfig option: {key}={value}. Ensure that 'value' is 'true' or 'false'.");
            }
        }
    }

    internal Config GetConfig() => this.lazyConfig.IsValueCreated ? this.lazyConfig.Value : null;
}
