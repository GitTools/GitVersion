using GitVersion.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion;

internal class OverrideConfigurationOptionParser
{
    private readonly Dictionary<object, object?> overrideConfiguration = [];

    private static readonly Lazy<ILookup<string?, PropertyInfo>> _lazySupportedProperties =
        new(GetSupportedProperties, true);

    internal static ILookup<string?, PropertyInfo> SupportedProperties => _lazySupportedProperties.Value;

    private static readonly Lazy<HashSet<string>> _lazySupportedBranchProperties =
        new(GetSupportedBranchProperties, true);

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

    internal static bool TryValidate(string key, out string? error)
    {
        var version = ConfigurationVersionSelector.Resolve();
        var segments = key.Split('.');
        if (IsValidPath(version, segments))
        {
            error = null;
            return true;
        }

        var replacement = GetReplacement(version, segments);
        error = replacement is null
            ? $"Unsupported key '{key}'."
            : $"Key '{key}' is not valid in the selected configuration structure. Use '{replacement}' instead. " +
              "Run 'gitversion config migrate' to convert a legacy configuration.";
        return false;
    }

    internal void SetValues(IReadOnlyCollection<string> values, string optionName)
    {
        List<(string Key, string Value)> parsedOptions = [];

        foreach (var keyValueOption in values)
        {
            var keyAndValue = QuotedStringHelpers.SplitUnquoted(keyValueOption, '=');
            if (keyAndValue.Length != 2)
            {
                throw new WarningException($"Could not parse {optionName} option: {keyValueOption}. Ensure it is in format 'key=value'.");
            }

            var optionKey = keyAndValue[0].ToLowerInvariant();
            if (!TryValidate(optionKey, out var error))
            {
                throw new WarningException($"Could not parse {optionName} option: {keyValueOption}. {error}");
            }

            parsedOptions.Add((optionKey, keyAndValue[1]));
        }

        foreach (var (key, value) in parsedOptions)
        {
            SetValue(key, value);
        }
    }

    internal void SetValue(string key, string value)
    {
        var segments = key.Split('.');
        IDictionary<object, object?> current = this.overrideConfiguration;
        for (var index = 0; index < segments.Length - 1; index++)
        {
            var segment = segments[index];
            if (!current.TryGetValue(segment, out var nested))
            {
                nested = new Dictionary<object, object?>();
                current[segment] = nested;
            }

            current = (IDictionary<object, object?>)nested!;
        }

        current[segments[^1]] = QuotedStringHelpers.UnquoteText(value);
    }

    internal IReadOnlyDictionary<object, object?> GetOverrideConfiguration() => this.overrideConfiguration;

    private static HashSet<string> GetSupportedBranchProperties() => typeof(BranchConfiguration)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => IsSupportedPropertyType(property.PropertyType) && property.CanWrite)
        .Select(property => property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name)
        .OfType<string>()
        .ToHashSet(StringComparer.Ordinal);

    private static bool IsValidPath(ConfigurationVersion version, IReadOnlyList<string> segments)
    {
        if (version == ConfigurationVersion.V6)
        {
            return segments.Count switch
            {
                1 => SupportedProperties.Contains(segments[0]),
                3 when segments[0] == ConfigurationDocumentMapper.BranchesPropertyName
                    => _lazySupportedBranchProperties.Value.Contains(segments[2]),
                _ => false
            };
        }

        if (segments.Count == 2 && IsSection(segments[0]))
        {
            return SupportedProperties.Contains(segments[1])
                   && IsCorrectSection(segments[0], segments[1], branchProperty: false);
        }

        return segments.Count == 4
               && IsSection(segments[0])
               && segments[1] == ConfigurationDocumentMapper.BranchesPropertyName
               && _lazySupportedBranchProperties.Value.Contains(segments[3])
               && IsCorrectSection(segments[0], segments[3], branchProperty: true);
    }

    private static string? GetReplacement(ConfigurationVersion version, IReadOnlyList<string> segments) =>
        version == ConfigurationVersion.V7
            ? GetV7Replacement(segments)
            : GetV6Replacement(segments);

    private static string? GetV7Replacement(IReadOnlyList<string> segments)
    {
        if (IsFlatRootPath(segments))
        {
            return $"{GetSection(segments[0], branchProperty: false)}.{segments[0]}";
        }

        if (IsFlatBranchPath(segments))
        {
            return $"{GetSection(segments[2], branchProperty: true)}.{string.Join('.', segments)}";
        }

        if (IsNestedRootPath(segments))
        {
            return $"{GetSection(segments[1], branchProperty: false)}.{segments[1]}";
        }

        return IsNestedBranchPath(segments)
            ? $"{GetSection(segments[3], branchProperty: true)}.{string.Join('.', segments.Skip(1))}"
            : null;
    }

    private static string? GetV6Replacement(IReadOnlyList<string> segments)
    {
        if (IsNestedRootPath(segments))
        {
            return segments[1];
        }

        return IsNestedBranchPath(segments) ? string.Join('.', segments.Skip(1)) : null;
    }

    private static bool IsFlatRootPath(IReadOnlyList<string> segments) =>
        segments.Count == 1 && SupportedProperties.Contains(segments[0]);

    private static bool IsFlatBranchPath(IReadOnlyList<string> segments) =>
        segments.Count == 3
        && segments[0] == ConfigurationDocumentMapper.BranchesPropertyName
        && _lazySupportedBranchProperties.Value.Contains(segments[2]);

    private static bool IsNestedRootPath(IReadOnlyList<string> segments) =>
        segments.Count == 2
        && IsSection(segments[0])
        && SupportedProperties.Contains(segments[1]);

    private static bool IsNestedBranchPath(IReadOnlyList<string> segments) =>
        segments.Count == 4
        && IsSection(segments[0])
        && segments[1] == ConfigurationDocumentMapper.BranchesPropertyName
        && _lazySupportedBranchProperties.Value.Contains(segments[3]);

    private static bool IsSection(string value)
        => value is ConfigurationDocumentMapper.CalculationSectionName or ConfigurationDocumentMapper.OutputSectionName;

    private static bool IsCorrectSection(string section, string propertyName, bool branchProperty)
        => section == GetSection(propertyName, branchProperty);

    private static string GetSection(string propertyName, bool branchProperty)
    {
        var isOutput = branchProperty
            ? ConfigurationDocumentMapper.IsOutputBranchProperty(propertyName)
            : ConfigurationDocumentMapper.IsOutputProperty(propertyName);
        return isOutput ? ConfigurationDocumentMapper.OutputSectionName : ConfigurationDocumentMapper.CalculationSectionName;
    }
}
