using GitVersion.Output.Attributes;

namespace GitVersion.OutputVariables;

internal class VersionVariablesJsonModel
{
    [JsonPropertyDescription("Suitable for .NET AssemblyFileVersion. Defaults to Major.Minor.Patch.0.")]
    public string? AssemblySemFileVer { get; set; }

    [JsonPropertyDescription("Suitable for .NET AssemblyVersion. Defaults to Major.Minor.0.0")]
    public string? AssemblySemVer { get; set; }

    [JsonPropertyDescription("The name of the checked out Git branch.")]
    public string? BranchName { get; set; }

    [JsonPropertyDescription("The build metadata, usually representing number of commits since the CommitCountSourceSha.")]
    public int? BuildMetaData { get; set; }

    [JsonPropertyDescription("The number of non-ignored commits reachable from HEAD but not from the counting anchor.")]
    public long? CommitCountSourceDistance { get; set; }

    [JsonPropertyDescription("The commit used as the counting anchor, or null when counting all reachable history.")]
    [JsonConverter(typeof(SourceVariableJsonConverter))]
    public string? CommitCountSourceSha { get; set; }

    [JsonPropertyDescription("The ISO-8601 formatted date of the commit identified by Sha.")]
    public string? CommitDate { get; set; }

    [JsonPropertyDescription("A custom version computed from custom-version-format when configured.")]
    public string? CustomVersion { get; set; }

    [JsonPropertyDescription("Equal to BranchName, but with / replaced with -.")]
    public string? EscapedBranchName { get; set; }

    [JsonPropertyDescription("The BuildMetaData suffixed with BranchName and Sha.")]
    public string? FullBuildMetaData { get; set; }

    [JsonPropertyDescription("The full, SemVer 2.0 compliant version number.")]
    public string? FullSemVer { get; set; }

    [JsonPropertyDescription("Suitable for .NET AssemblyInformationalVersion. Defaults to FullSemVer suffixed by FullBuildMetaData.")]
    public string? InformationalVersion { get; set; }

    [JsonPropertyDescription("The major version. Should be incremented on breaking changes.")]
    public int? Major { get; set; }

    [JsonPropertyDescription("Major, Minor and Patch joined together, separated by '.'.")]
    public string? MajorMinorPatch { get; set; }

    [JsonPropertyDescription("The minor version. Should be incremented on new features.")]
    public int? Minor { get; set; }

    [JsonPropertyDescription("The patch version. Should be incremented on bug fixes.")]
    public int? Patch { get; set; }

    [JsonPropertyDescription("The name of the pre-release label, without the PreReleaseNumber.")]
    public string? PreReleaseLabelName { get; set; }

    [JsonPropertyDescription("The pre-release label name prefixed with a dash.")]
    public string? PreReleaseLabelNameWithDash { get; set; }

    [JsonPropertyDescription("The pre-release number is the number of commits since the last version bump.")]
    public int? PreReleaseNumber { get; set; }

    [JsonPropertyDescription("The full pre-release label, including the PreReleaseNumber when present.")]
    public string? PreReleaseLabel { get; set; }

    [JsonPropertyDescription("The pre-release label prefixed with a dash.")]
    public string? PreReleaseLabelWithDash { get; set; }

    [JsonPropertyDescription("The semantic version number, including PreReleaseLabelWithDash for pre-release version numbers.")]
    public string? SemVer { get; set; }

    [JsonPropertyDescription("The final increment relative to the semantic baseline: None, Patch, Minor or Major.")]
    [JsonConverter(typeof(SourceVariableJsonConverter))]
    public string? SemVerSourceIncrement { get; set; }

    [JsonPropertyDescription("The semantic baseline supplied by the selected artifact or derivation.")]
    [JsonConverter(typeof(SourceVariableJsonConverter))]
    public string? SemVerSourceSemVer { get; set; }

    [JsonPropertyDescription("The semantic source commit SHA, or null for external sources such as configuration or a branch name.")]
    [JsonConverter(typeof(SourceVariableJsonConverter))]
    public string? SemVerSourceSha { get; set; }

    [JsonPropertyDescription("The SHA of the Git commit.")]
    public string? Sha { get; set; }

    [JsonPropertyDescription("The Sha limited to 7 characters.")]
    public string? ShortSha { get; set; }

    [JsonPropertyDescription("The number of uncommitted changes present in the repository.")]
    public int? UncommittedChanges { get; set; }

    [JsonPropertyDescription("Compatibility alias for CommitCountSourceDistance.")]
    public int? VersionSourceDistance { get; set; }

    [JsonPropertyDescription("The increment strategy used for the version calculation. Possible values: None, Patch, Minor, Major.")]
    public string? VersionSourceIncrement { get; set; }

    [JsonPropertyDescription("Legacy semantic baseline; it need not come from VersionSourceSha. Prefer SemVerSourceSemVer.")]
    public string? VersionSourceSemVer { get; set; }

    [JsonPropertyDescription("Compatibility alias for CommitCountSourceSha.")]
    public string? VersionSourceSha { get; set; }

    [JsonPropertyDescription("A summation of branch specific pre-release-weight and the PreReleaseNumber. Can be used to obtain a monotonically increasing version number across the branches.")]
    public int? WeightedPreReleaseNumber { get; set; }
}
