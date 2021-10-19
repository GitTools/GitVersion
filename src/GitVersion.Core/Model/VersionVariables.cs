using System.Text.Encodings.Web;
using System.Text.Json;
using GitVersion.Helpers;
using GitVersion.Logging;
using YamlDotNet.Serialization;
using static GitVersion.Extensions.ObjectExtensions;

namespace GitVersion.OutputVariables;

public class VersionVariables : IEnumerable<KeyValuePair<string, string>>
{
    public VersionVariables(string major,
        string minor,
        string patch,
        string? buildMetaData,
        string? buildMetaDataPadded,
        string? fullBuildMetaData,
        string? branchName,
        string? escapedBranchName,
        string? sha,
        string? shortSha,
        string majorMinorPatch,
        string semVer,
        string legacySemVer,
        string legacySemVerPadded,
        string fullSemVer,
        string? assemblySemVer,
        string? assemblySemFileVer,
        string? preReleaseTag,
        string? preReleaseTagWithDash,
        string? preReleaseLabel,
        string? preReleaseLabelWithDash,
        string? preReleaseNumber,
        string weightedPreReleaseNumber,
        string? informationalVersion,
        string? commitDate,
        string nugetVersion,
        string nugetVersionV2,
        string? nugetPreReleaseTag,
        string? nugetPreReleaseTagV2,
        string? versionSourceSha,
        string? commitsSinceVersionSource,
        string? commitsSinceVersionSourcePadded,
        string? uncommittedChanges)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        BuildMetaData = buildMetaData;
        BuildMetaDataPadded = buildMetaDataPadded;
        FullBuildMetaData = fullBuildMetaData;
        BranchName = branchName;
        EscapedBranchName = escapedBranchName;
        Sha = sha;
        ShortSha = shortSha;
        MajorMinorPatch = majorMinorPatch;
        SemVer = semVer;
        LegacySemVer = legacySemVer;
        LegacySemVerPadded = legacySemVerPadded;
        FullSemVer = fullSemVer;
        AssemblySemVer = assemblySemVer;
        AssemblySemFileVer = assemblySemFileVer;
        PreReleaseTag = preReleaseTag;
        PreReleaseTagWithDash = preReleaseTagWithDash;
        PreReleaseLabel = preReleaseLabel;
        PreReleaseLabelWithDash = preReleaseLabelWithDash;
        PreReleaseNumber = preReleaseNumber;
        WeightedPreReleaseNumber = weightedPreReleaseNumber;
        InformationalVersion = informationalVersion;
        CommitDate = commitDate;
        NuGetVersion = nugetVersion;
        NuGetVersionV2 = nugetVersionV2;
        NuGetPreReleaseTag = nugetPreReleaseTag;
        NuGetPreReleaseTagV2 = nugetPreReleaseTagV2;
        VersionSourceSha = versionSourceSha;
        CommitsSinceVersionSource = commitsSinceVersionSource;
        CommitsSinceVersionSourcePadded = commitsSinceVersionSourcePadded;
        UncommittedChanges = uncommittedChanges;
    }

    public string Major { get; }
    public string Minor { get; }
    public string Patch { get; }
    public string? PreReleaseTag { get; }
    public string? PreReleaseTagWithDash { get; }
    public string? PreReleaseLabel { get; }
    public string? PreReleaseLabelWithDash { get; }
    public string? PreReleaseNumber { get; }
    public string WeightedPreReleaseNumber { get; }
    public string? BuildMetaData { get; }
    public string? BuildMetaDataPadded { get; }
    public string? FullBuildMetaData { get; }
    public string MajorMinorPatch { get; }
    public string SemVer { get; }
    public string LegacySemVer { get; }
    public string LegacySemVerPadded { get; }
    public string? AssemblySemVer { get; }
    public string? AssemblySemFileVer { get; }
    public string FullSemVer { get; }
    public string? InformationalVersion { get; }
    public string? BranchName { get; }
    public string? EscapedBranchName { get; }
    public string? Sha { get; }
    public string? ShortSha { get; }
    public string NuGetVersionV2 { get; }
    public string NuGetVersion { get; }
    public string? NuGetPreReleaseTagV2 { get; }
    public string? NuGetPreReleaseTag { get; }
    public string? VersionSourceSha { get; }
    public string? CommitsSinceVersionSource { get; }
    public string? CommitsSinceVersionSourcePadded { get; }
    public string? UncommittedChanges { get; }
    public string? CommitDate { get; set; }

    [ReflectionIgnore]
    public static IEnumerable<string> AvailableVariables => typeof(VersionVariables)
        .GetProperties()
        .Where(p => !p.GetCustomAttributes(typeof(ReflectionIgnoreAttribute), false).Any())
        .Select(p => p.Name)
        .OrderBy(a => a, StringComparer.Ordinal);

    [ReflectionIgnore]
    public string? FileName { get; set; }

    [ReflectionIgnore]
    public string? this[string variable] => typeof(VersionVariables).GetProperty(variable)?.GetValue(this, null) as string;

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => this.GetProperties().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // FIX ME: Shall we return an instance with no ctorArgs or explicitly fail when properties is null?
    public static VersionVariables FromDictionary(IEnumerable<KeyValuePair<string, string>>? properties)
    {
        var type = typeof(VersionVariables);
        var constructors = type.GetConstructors();

        var ctor = constructors.Single();
        var ctorArgs = ctor.GetParameters()
            .Select(p => properties.Single(v => string.Equals(v.Key, p.Name, StringComparison.InvariantCultureIgnoreCase)).Value)
            .Cast<object>()
            .ToArray();
        return (VersionVariables)Activator.CreateInstance(type, ctorArgs);
    }

    public static VersionVariables FromJson(string json)
    {
        var serializeOptions = JsonSerializerOptions();
        var variablePairs = JsonSerializer.Deserialize<Dictionary<string, string>>(json, serializeOptions);
        return FromDictionary(variablePairs);
    }

    public static VersionVariables FromFile(string filePath, IFileSystem fileSystem, ILog log)
    {
        try
        {
            if (log == null)
            {
                return FromFileInternal(filePath, fileSystem);
            }
            var retryAction = new RetryAction<IOException, VersionVariables>();
            return retryAction.Execute(() => FromFileInternal(filePath, fileSystem));
        }
        catch (AggregateException ex)
        {
            var lastException = ex.InnerExceptions?.LastOrDefault() ?? ex.InnerException;
            if (lastException != null)
            {
                throw lastException;
            }
            throw;
        }
    }
    private static VersionVariables FromFileInternal(string filePath, IFileSystem fileSystem)
    {
        using var stream = fileSystem.OpenRead(filePath);
        using var reader = new StreamReader(stream);
        var dictionary = new Deserializer().Deserialize<Dictionary<string, string>>(reader);
        var versionVariables = FromDictionary(dictionary);
        versionVariables.FileName = filePath;
        return versionVariables;
    }

    public bool TryGetValue(string variable, out string? variableValue)
    {
        if (ContainsKey(variable))
        {
            variableValue = this[variable];
            return true;
        }

        variableValue = null;
        return false;
    }

    private static bool ContainsKey(string variable) => typeof(VersionVariables).GetProperty(variable) != null;

    public override string ToString()
    {
        var variablesType = typeof(VersionVariablesJsonModel);
        var variables = new VersionVariablesJsonModel();

        foreach (KeyValuePair<string, string> property in this.GetProperties())
        {
            variablesType.GetProperty(property.Key).SetValue(variables, property.Value);
        }

        var serializeOptions = JsonSerializerOptions();

        return JsonSerializer.Serialize(variables, serializeOptions);
    }

    private static JsonSerializerOptions JsonSerializerOptions()
    {
        var serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new VersionVariablesJsonStringConverter() }
        };
        return serializeOptions;
    }
}
