namespace GitVersion.Configuration;

internal enum ConfigurationDocumentKind
{
    Empty,
    V6,
    V7,
    Mixed
}

internal static class ConfigurationDocumentMapper
{
    public const string CalculationSectionName = "calculation";
    public const string OutputSectionName = "output";
    public const string BranchesPropertyName = "branches";

    private static readonly HashSet<string> OutputPropertyNames =
    [
        "assembly-file-versioning-format",
        "assembly-file-versioning-scheme",
        "assembly-informational-format",
        "assembly-versioning-format",
        "assembly-versioning-scheme",
        "commit-date-format",
        "custom-version-format",
        "pre-release-weight",
        "tag-pre-release-weight",
        "update-build-number"
    ];

    private static readonly HashSet<string> OutputBranchPropertyNames =
    [
        "custom-version-format",
        "pre-release-weight"
    ];

    private static readonly HashSet<string> KnownPropertyNames = typeof(GitVersionConfiguration)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(property => property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name)
        .OfType<string>()
        .ToHashSet(StringComparer.Ordinal);

    private static readonly HashSet<string> KnownBranchPropertyNames = typeof(BranchConfiguration)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(property => property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name)
        .OfType<string>()
        .ToHashSet(StringComparer.Ordinal);

    public static ConfigurationDocumentKind Detect(IReadOnlyDictionary<object, object?> document)
    {
        if (document.Count == 0)
        {
            return ConfigurationDocumentKind.Empty;
        }

        var hasNested = document.ContainsKey(CalculationSectionName) || document.ContainsKey(OutputSectionName);
        var hasFlat = document.Keys.OfType<string>().Any(key =>
            !key.Equals(CalculationSectionName, StringComparison.Ordinal)
            && !key.Equals(OutputSectionName, StringComparison.Ordinal));

        return (hasNested, hasFlat) switch
        {
            (true, true) => ConfigurationDocumentKind.Mixed,
            (true, false) => ConfigurationDocumentKind.V7,
            _ => ConfigurationDocumentKind.V6
        };
    }

    public static Dictionary<object, object?> Normalize(
        IReadOnlyDictionary<object, object?> document,
        ConfigurationVersion selectedVersion,
        string source)
    {
        var kind = Detect(document);
        if (kind == ConfigurationDocumentKind.Mixed)
        {
            throw new ConfigurationException(
                $"The {source} mixes the v6 flat configuration structure with the v7 'calculation'/'output' structure. " +
                "Use only one structure. Run 'gitversion config migrate' to convert a v6 configuration.");
        }

        if (kind == ConfigurationDocumentKind.V6 && selectedVersion == ConfigurationVersion.V7)
        {
            throw new ConfigurationException(
                $"The {source} uses the legacy v6 configuration structure, but {ConfigurationVersionSelector.EnvironmentVariableName}=v7 is selected. " +
                $"Run 'gitversion config migrate' or set {ConfigurationVersionSelector.EnvironmentVariableName}=v6 temporarily.");
        }

        if (kind == ConfigurationDocumentKind.V7 && selectedVersion == ConfigurationVersion.V6)
        {
            throw new ConfigurationException(
                $"The {source} uses the v7 configuration structure, but {ConfigurationVersionSelector.EnvironmentVariableName}=v6 is selected. " +
                $"Remove the override or set {ConfigurationVersionSelector.EnvironmentVariableName}=v7.");
        }

        return kind == ConfigurationDocumentKind.V7 ? Flatten(document) : CloneDictionary(document);
    }

    public static Dictionary<object, object?> NormalizeInternal(IReadOnlyDictionary<object, object?> document, string source)
    {
        var kind = Detect(document);
        return kind switch
        {
            ConfigurationDocumentKind.Empty => [],
            ConfigurationDocumentKind.V6 => CloneDictionary(document),
            ConfigurationDocumentKind.V7 => Flatten(document),
            _ => throw new ConfigurationException(
                $"The {source} mixes the v6 flat configuration structure with the v7 'calculation'/'output' structure.")
        };
    }

    public static Dictionary<object, object?> Flatten(IReadOnlyDictionary<object, object?> document)
    {
        Dictionary<object, object?> result = [];
        Dictionary<object, object?> branches = [];

        FlattenSection(document, CalculationSectionName, result, branches);
        FlattenSection(document, OutputSectionName, result, branches);

        if (branches.Count != 0)
        {
            result[BranchesPropertyName] = branches;
        }

        return result;
    }

    public static Dictionary<string, object?> Nest(IReadOnlyDictionary<string, object?> document)
    {
        Dictionary<string, object?> calculation = [];
        Dictionary<string, object?> output = [];

        foreach (var (key, value) in document)
        {
            if (key.Equals(BranchesPropertyName, StringComparison.Ordinal))
            {
                SplitBranches(value, calculation, output);
            }
            else if (OutputPropertyNames.Contains(key))
            {
                output[key] = value;
            }
            else
            {
                calculation[key] = value;
            }
        }

        return new Dictionary<string, object?>
        {
            [CalculationSectionName] = calculation,
            [OutputSectionName] = output
        };
    }

    public static bool IsOutputProperty(string propertyName) => OutputPropertyNames.Contains(propertyName);

    public static bool IsOutputBranchProperty(string propertyName) => OutputBranchPropertyNames.Contains(propertyName);

    private static void FlattenSection(
        IReadOnlyDictionary<object, object?> document,
        string sectionName,
        IDictionary<object, object?> result,
        IDictionary<object, object?> branches)
    {
        if (!document.TryGetValue(sectionName, out var sectionValue) || sectionValue is null)
        {
            return;
        }

        if (sectionValue is not IReadOnlyDictionary<object, object?> section)
        {
            throw new ConfigurationException($"Configuration section '{sectionName}' must be a mapping.");
        }

        foreach (var (key, value) in section)
        {
            if (key is string propertyName && propertyName.Equals(BranchesPropertyName, StringComparison.Ordinal))
            {
                MergeBranches(branches, value, sectionName);
                continue;
            }

            if (key is string configuredPropertyName)
            {
                ValidatePropertyOwnership(sectionName, configuredPropertyName, branchProperty: false);
            }

            if (!result.TryAdd(key, CloneValue(value)))
            {
                throw new ConfigurationException(
                    $"Configuration property '{key}' is defined in both '{CalculationSectionName}' and '{OutputSectionName}'.");
            }
        }
    }

    private static void MergeBranches(IDictionary<object, object?> target, object? value, string sectionName)
    {
        if (value is not IReadOnlyDictionary<object, object?> source)
        {
            throw new ConfigurationException($"Configuration property '{sectionName}.{BranchesPropertyName}' must be a mapping.");
        }

        foreach (var (branchName, branchValue) in source)
        {
            if (branchValue is not IReadOnlyDictionary<object, object?> branch)
            {
                throw new ConfigurationException(
                    $"Configuration branch '{sectionName}.{BranchesPropertyName}.{branchName}' must be a mapping.");
            }

            foreach (var propertyName in branch.Keys.OfType<string>())
            {
                ValidatePropertyOwnership(sectionName, propertyName, branchProperty: true);
            }

            if (!target.TryGetValue(branchName, out var existing))
            {
                target[branchName] = CloneDictionary(branch);
                continue;
            }

            if (existing is not IDictionary<object, object?> targetBranch)
            {
                throw new ConfigurationException($"Configuration branch '{branchName}' must be a mapping.");
            }

            foreach (var (propertyName, propertyValue) in branch)
            {
                if (!targetBranch.TryAdd(propertyName, CloneValue(propertyValue)))
                {
                    throw new ConfigurationException(
                        $"Configuration branch property '{branchName}.{propertyName}' is defined in both " +
                        $"'{CalculationSectionName}' and '{OutputSectionName}'.");
                }
            }
        }
    }

    private static void SplitBranches(
        object? value,
        IDictionary<string, object?> calculation,
        IDictionary<string, object?> output)
    {
        if (value is not IReadOnlyDictionary<string, object?> branches)
        {
            return;
        }

        Dictionary<string, object?> calculationBranches = [];
        Dictionary<string, object?> outputBranches = [];

        foreach (var (branchName, branchValue) in branches)
        {
            if (branchValue is not IReadOnlyDictionary<string, object?> branch)
            {
                continue;
            }

            Dictionary<string, object?> calculationBranch = [];
            Dictionary<string, object?> outputBranch = [];
            foreach (var (propertyName, propertyValue) in branch)
            {
                (OutputBranchPropertyNames.Contains(propertyName) ? outputBranch : calculationBranch)[propertyName] = propertyValue;
            }

            if (calculationBranch.Count != 0)
            {
                calculationBranches[branchName] = calculationBranch;
            }

            if (outputBranch.Count != 0)
            {
                outputBranches[branchName] = outputBranch;
            }
        }

        if (calculationBranches.Count != 0)
        {
            calculation[BranchesPropertyName] = calculationBranches;
        }

        if (outputBranches.Count != 0)
        {
            output[BranchesPropertyName] = outputBranches;
        }
    }

    private static Dictionary<object, object?> CloneDictionary(IReadOnlyDictionary<object, object?> dictionary)
        => dictionary.ToDictionary(item => item.Key, item => CloneValue(item.Value));

    private static void ValidatePropertyOwnership(string sectionName, string propertyName, bool branchProperty)
    {
        var knownProperties = branchProperty ? KnownBranchPropertyNames : KnownPropertyNames;
        if (!knownProperties.Contains(propertyName))
        {
            return;
        }

        var belongsToOutput = branchProperty
            ? OutputBranchPropertyNames.Contains(propertyName)
            : OutputPropertyNames.Contains(propertyName);
        var expectedSection = belongsToOutput ? OutputSectionName : CalculationSectionName;
        if (!sectionName.Equals(expectedSection, StringComparison.Ordinal))
        {
            var branchPath = branchProperty ? $"{BranchesPropertyName}.<branch>." : string.Empty;
            throw new ConfigurationException(
                $"Configuration property '{sectionName}.{branchPath}{propertyName}' belongs under " +
                $"'{expectedSection}.{branchPath}{propertyName}'.");
        }
    }

    private static object? CloneValue(object? value) => value switch
    {
        IReadOnlyDictionary<object, object?> dictionary => CloneDictionary(dictionary),
        _ => value
    };
}
