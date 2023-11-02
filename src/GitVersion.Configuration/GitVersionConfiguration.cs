using System.Globalization;
using System.Text.RegularExpressions;
using GitVersion.Attributes;
using GitVersion.Extensions;

namespace GitVersion.Configuration;

internal sealed record GitVersionConfiguration : BranchConfiguration, IGitVersionConfiguration
{
    [JsonPropertyName("workflow")]
    [JsonPropertyDescription("The base template of the configuration to use. Possible values are: GitFlow/v1 or GitHubFlow/v1")]
    public string? Workflow { get; internal set; }

    [JsonPropertyName("assembly-versioning-scheme")]
    [JsonPropertyDescription("The scheme to use when setting AssemblyVersion attribute. Can be 'MajorMinorPatchTag', 'MajorMinorPatch', 'MajorMinor', 'Major', 'None'.")]
    public AssemblyVersioningScheme? AssemblyVersioningScheme { get; internal set; }

    [JsonPropertyName("assembly-file-versioning-scheme")]
    [JsonPropertyDescription("The scheme to use when setting AssemblyFileVersion attribute. Can be 'MajorMinorPatchTag', 'MajorMinorPatch', 'MajorMinor', 'Major', 'None'.")]
    public AssemblyFileVersioningScheme? AssemblyFileVersioningScheme { get; internal set; }

    [JsonPropertyName("assembly-informational-format")]
    [JsonPropertyDescription("Specifies the format of AssemblyInformationalVersion. The default value is {InformationalVersion}.")]
    public string? AssemblyInformationalFormat { get; internal set; }

    [JsonPropertyName("assembly-versioning-format")]
    [JsonPropertyDescription("Specifies the format of AssemblyVersion and overwrites the value of assembly-versioning-scheme.")]
    public string? AssemblyVersioningFormat { get; internal set; }

    [JsonPropertyName("assembly-file-versioning-format")]
    [JsonPropertyDescription("Specifies the format of AssemblyFileVersion and overwrites the value of assembly-file-versioning-scheme.")]
    public string? AssemblyFileVersioningFormat { get; internal set; }

    [JsonPropertyName("tag-prefix")]
    [JsonPropertyDescription($"A regular expression which is used to trim Git tags before processing. Defaults to {ConfigurationConstants.DefaultTagPrefix}")]
    public string? TagPrefix { get; internal set; }

    [JsonPropertyName("version-in-branch-pattern")]
    [JsonPropertyDescription($"A regular expression which is used to determine the version number in the branch name or commit message (e.g., v1.0.0-LTS). The default value is '{ConfigurationConstants.DefaultVersionInBranchPattern}'.")]
    public string? VersionInBranchPattern { get; internal set; }

    [JsonIgnore]
    public Regex VersionInBranchRegex => versionInBranchRegex ??= new Regex(GetVersionInBranchPattern(), RegexOptions.Compiled);
    private Regex? versionInBranchRegex;

    private string GetVersionInBranchPattern()
    {
        var versionInBranchPattern = VersionInBranchPattern;
        if (versionInBranchPattern.IsNullOrEmpty()) versionInBranchPattern = ConfigurationConstants.DefaultVersionInBranchPattern;
        return $"^{versionInBranchPattern.TrimStart('^')}";
    }

    [JsonPropertyName("next-version")]
    [JsonPropertyDescription("Allows you to bump the next version explicitly. Useful for bumping main or a feature branch with breaking changes")]
    public string? NextVersion
    {
        get => nextVersion;
        internal set =>
            nextVersion = int.TryParse(value, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var major)
                ? $"{major}.0"
                : value;
    }
    private string? nextVersion;

    [JsonPropertyName("major-version-bump-message")]
    [JsonPropertyDescription(@"The regular expression to match commit messages with to perform a major version increment. Default set to '\+semver:\s?(breaking|major)'")]
    [JsonPropertyDefault(@"'\+semver:\s?(breaking|major)'")]
    public string? MajorVersionBumpMessage { get; internal set; }

    [JsonPropertyName("minor-version-bump-message")]
    [JsonPropertyDescription(@"The regular expression to match commit messages with to perform a minor version increment. Default set to '\+semver:\s?(feature|minor)'")]
    [JsonPropertyDefault(@"'\+semver:\s?(feature|minor)'")]
    public string? MinorVersionBumpMessage { get; internal set; }

    [JsonPropertyName("patch-version-bump-message")]
    [JsonPropertyDescription(@"The regular expression to match commit messages with to perform a patch version increment. Default set to '\+semver:\s?(fix|patch)'")]
    [JsonPropertyDefault(@"'\+semver:\s?(fix|patch)'")]
    public string? PatchVersionBumpMessage { get; internal set; }

    [JsonPropertyName("no-bump-message")]
    [JsonPropertyDescription(@"Used to tell GitVersion not to increment when in Mainline development mode. . Default set to '\+semver:\s?(none|skip)'")]
    [JsonPropertyDefault(@"'\+semver:\s?(none|skip)'")]
    public string? NoBumpMessage { get; internal set; }

    [JsonPropertyName("tag-pre-release-weight")]
    [JsonPropertyDescription("The pre-release weight in case of tagged commits. Defaults to 60000.")]
    public int? TagPreReleaseWeight { get; internal set; }

    [JsonPropertyName("commit-date-format")]
    [JsonPropertyDescription("The format to use when calculating the commit date. Defaults to 'yyyy-MM-dd'. See [Standard Date and Time Format Strings](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings) and [Custom Date and Time Format Strings](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings).")]
    [JsonPropertyDefault("'yyyy-MM-dd'")]
    public string? CommitDateFormat { get; internal set; }

    [JsonPropertyName("merge-message-formats")]
    [JsonPropertyDescription("Custom merge message formats to enable identification of merge messages that do not follow the built-in conventions.")]
    public Dictionary<string, string> MergeMessageFormats { get; internal set; } = new();

    [JsonIgnore]
    IReadOnlyDictionary<string, string> IGitVersionConfiguration.MergeMessageFormats => MergeMessageFormats;

    [JsonPropertyName("update-build-number")]
    [JsonPropertyDescription("Whether to update the build number in the project file. Defaults to true.")]
    public bool UpdateBuildNumber { get; internal set; } = true;

    [JsonPropertyName("semantic-version-format")]
    [JsonPropertyDescription("Specifies the semantic version format that is used when parsing the string. Can be 'Strict' or 'Loose'.")]
    public SemanticVersionFormat SemanticVersionFormat { get; internal set; }

    [JsonIgnore]
    IReadOnlyDictionary<string, IBranchConfiguration> IGitVersionConfiguration.Branches
        => Branches.ToDictionary(element => element.Key, element => (IBranchConfiguration)element.Value);

    [JsonPropertyName("branches")]
    [JsonPropertyDescription("The header for all the individual branch configuration.")]
    public Dictionary<string, BranchConfiguration> Branches { get; internal set; } = new();

    [JsonIgnore]
    IIgnoreConfiguration IGitVersionConfiguration.Ignore => Ignore;

    [JsonPropertyName("ignore")]
    [JsonPropertyDescription("The header property for the ignore configuration.")]
    public IgnoreConfiguration Ignore { get; internal set; } = new();

    public override IBranchConfiguration Inherit(IBranchConfiguration configuration) => throw new NotSupportedException();

    public string ToJsonString()
    {
        var stringBuilder = new StringBuilder();
        using var stream = new StringWriter(stringBuilder);
        ConfigurationSerializer.Write(this, stream);
        stream.Flush();
        return stringBuilder.ToString();
    }
}
