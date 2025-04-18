using System.Globalization;
using GitVersion.Configuration.Attributes;
using GitVersion.Core;
using GitVersion.VersionCalculation;
using static GitVersion.Configuration.ConfigurationConstants;

namespace GitVersion.Configuration;

internal sealed record GitVersionConfiguration : BranchConfiguration, IGitVersionConfiguration
{
    [JsonPropertyName("workflow")]
    [JsonPropertyDescription("The base template of the configuration to use. Possible values are: 'GitFlow/v1' or 'GitHubFlow/v1'")]
    public string? Workflow { get; internal set; }

    [JsonPropertyName("assembly-versioning-scheme")]
    [JsonPropertyDescription($"The scheme to use when setting AssemblyVersion attribute. Can be 'MajorMinorPatchTag', 'MajorMinorPatch', 'MajorMinor', 'Major', 'None'. Defaults to '{NameOfDefaultAssemblyVersioningScheme}'.")]
    [JsonPropertyDefault(DefaultAssemblyVersioningScheme)]
    public AssemblyVersioningScheme? AssemblyVersioningScheme { get; internal set; }

    [JsonPropertyName("assembly-file-versioning-scheme")]
    [JsonPropertyDescription($"The scheme to use when setting AssemblyFileVersion attribute. Can be 'MajorMinorPatchTag', 'MajorMinorPatch', 'MajorMinor', 'Major', 'None'. Defaults to '{NameOfDefaultAssemblyFileVersioningScheme}'.")]
    [JsonPropertyDefault(DefaultAssemblyFileVersioningScheme)]
    public AssemblyFileVersioningScheme? AssemblyFileVersioningScheme { get; internal set; }

    [JsonPropertyName("assembly-informational-format")]
    [JsonPropertyDescription($"Specifies the format of AssemblyInformationalVersion. Defaults to '{DefaultAssemblyInformationalFormat}'.")]
    [JsonPropertyDefault($"'{DefaultAssemblyInformationalFormat}'")]
    public string? AssemblyInformationalFormat { get; internal set; }

    [JsonPropertyName("assembly-versioning-format")]
    [JsonPropertyDescription("Specifies the format of AssemblyVersion and overwrites the value of assembly-versioning-scheme.")]
    public string? AssemblyVersioningFormat { get; internal set; }

    [JsonPropertyName("assembly-file-versioning-format")]
    [JsonPropertyDescription("Specifies the format of AssemblyFileVersion and overwrites the value of assembly-file-versioning-scheme.")]
    public string? AssemblyFileVersioningFormat { get; internal set; }

    [JsonPropertyName("tag-prefix")]
    [JsonPropertyDescription($"A regular expression which is used to trim Git tags before processing. Defaults to '{RegexPatterns.Configuration.DefaultTagPrefixPattern}'")]
    [JsonPropertyDefault(RegexPatterns.Configuration.DefaultTagPrefixPattern)]
    [JsonPropertyFormat(Format.Regex)]
    public string? TagPrefixPattern { get; internal set; }

    [JsonPropertyName("version-in-branch-pattern")]
    [JsonPropertyDescription($"A regular expression which is used to determine the version number in the branch name or commit message (e.g., v1.0.0-LTS). Defaults to '{RegexPatterns.Configuration.DefaultVersionInBranchPattern}'.")]
    [JsonPropertyDefault(RegexPatterns.Configuration.DefaultVersionInBranchPattern)]
    [JsonPropertyFormat(Format.Regex)]
    public string? VersionInBranchPattern { get; internal set; }

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
    [JsonPropertyDescription($"The regular expression to match commit messages with to perform a major version increment. Defaults to '{RegexPatterns.VersionCalculation.DefaultMajorPattern}'")]
    [JsonPropertyDefault(RegexPatterns.VersionCalculation.DefaultMajorPattern)]
    [JsonPropertyFormat(Format.Regex)]
    public string? MajorVersionBumpMessage { get; internal set; }

    [JsonPropertyName("minor-version-bump-message")]
    [JsonPropertyDescription($"The regular expression to match commit messages with to perform a minor version increment. Defaults to '{RegexPatterns.VersionCalculation.DefaultMinorPattern}'")]
    [JsonPropertyDefault(RegexPatterns.VersionCalculation.DefaultMinorPattern)]
    [JsonPropertyFormat(Format.Regex)]
    public string? MinorVersionBumpMessage { get; internal set; }

    [JsonPropertyName("patch-version-bump-message")]
    [JsonPropertyDescription($"The regular expression to match commit messages with to perform a patch version increment. Defaults to '{RegexPatterns.VersionCalculation.DefaultPatchPattern}'")]
    [JsonPropertyDefault(RegexPatterns.VersionCalculation.DefaultPatchPattern)]
    [JsonPropertyFormat(Format.Regex)]
    public string? PatchVersionBumpMessage { get; internal set; }

    [JsonPropertyName("no-bump-message")]
    [JsonPropertyDescription($"Used to tell GitVersion not to increment when in Mainline development mode. Defaults to '{RegexPatterns.VersionCalculation.DefaultNoBumpPattern}'")]
    [JsonPropertyDefault(RegexPatterns.VersionCalculation.DefaultNoBumpPattern)]
    [JsonPropertyFormat(Format.Regex)]
    public string? NoBumpMessage { get; internal set; }

    [JsonPropertyName("tag-pre-release-weight")]
    [JsonPropertyDescription($"The pre-release weight in case of tagged commits. Defaults to {StringDefaultTagPreReleaseWeight}.")]
    public int? TagPreReleaseWeight { get; internal set; }

    [JsonPropertyName("commit-date-format")]
    [JsonPropertyDescription($"The format to use when calculating the commit date. Defaults to '{DefaultCommitDateFormat}'. See [Standard Date and Time Format Strings](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings) and [Custom Date and Time Format Strings](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings).")]
    [JsonPropertyDefault(DefaultCommitDateFormat)]
    [System.Diagnostics.CodeAnalysis.StringSyntax("DateTimeFormat")]
    public string? CommitDateFormat { get; internal set; }

    [JsonPropertyName("merge-message-formats")]
    [JsonPropertyDescription("Custom merge message formats to enable identification of merge messages that do not follow the built-in conventions.")]
    public Dictionary<string, string> MergeMessageFormats { get; internal set; } = [];

    [JsonIgnore]
    IReadOnlyDictionary<string, string> IGitVersionConfiguration.MergeMessageFormats => MergeMessageFormats;

    [JsonPropertyName("update-build-number")]
    [JsonPropertyDescription($"Whether to update the build number in the project file. Defaults to {StringDefaultUpdateBuildNumber}.")]
    [JsonPropertyDefault(DefaultUpdateBuildNumber)]
    public bool UpdateBuildNumber { get; internal set; } = DefaultUpdateBuildNumber;

    [JsonPropertyName("semantic-version-format")]
    [JsonPropertyDescription($"Specifies the semantic version format that is used when parsing the string. Can be 'Strict' or 'Loose'. Defaults to '{StringDefaultSemanticVersionFormat}'.")]
    [JsonPropertyDefault(DefaultSemanticVersionFormat)]
    public SemanticVersionFormat SemanticVersionFormat { get; internal set; }

    [JsonIgnore]
    VersionStrategies IGitVersionConfiguration.VersionStrategy => VersionStrategies.Length == 0
        ? VersionCalculation.VersionStrategies.None : VersionStrategies.Aggregate((one, another) => one | another);

    [JsonPropertyName("strategies")]
    [JsonPropertyDescription($"Specifies which version strategies (one or more) will be used to determine the next version. Following values are available: 'ConfiguredNextVersion', 'MergeMessage', 'TaggedCommit', 'TrackReleaseBranches', 'VersionInBranchName' and 'Mainline'.")]
    public VersionStrategies[] VersionStrategies { get; internal set; } = [];

    [JsonIgnore]
    IReadOnlyDictionary<string, IBranchConfiguration> IGitVersionConfiguration.Branches
        => Branches.ToDictionary(element => element.Key, IBranchConfiguration (element) => element.Value);

    [JsonPropertyName("branches")]
    [JsonPropertyDescription("The header for all the individual branch configuration.")]
    public Dictionary<string, BranchConfiguration> Branches { get; internal set; } = [];

    [JsonIgnore]
    IIgnoreConfiguration IGitVersionConfiguration.Ignore => Ignore;

    [JsonPropertyName("ignore")]
    [JsonPropertyDescription("The header property for the ignore configuration.")]
    public IgnoreConfiguration Ignore { get; internal set; } = new();

    public override IBranchConfiguration Inherit(IBranchConfiguration configuration) => throw new NotSupportedException();

    public override IBranchConfiguration Inherit(EffectiveConfiguration configuration) => throw new NotSupportedException();

    public IBranchConfiguration GetEmptyBranchConfiguration() => new BranchConfiguration
    {
        RegularExpression = string.Empty,
        Label = BranchNamePlaceholder,
        Increment = IncrementStrategy.Inherit
    };
}
