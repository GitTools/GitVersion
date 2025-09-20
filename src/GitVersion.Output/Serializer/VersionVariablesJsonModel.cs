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

    [JsonPropertyDescription("The build metadata, usually representing number of commits since the VersionSourceSha.")]
    public int? BuildMetaData { get; set; }

    [JsonPropertyDescription("The ISO-8601 formatted date of the commit identified by Sha.")]
    public string? CommitDate { get; set; }

    [JsonPropertyDescription("The number of commits since the version source.")]
    public int? CommitsSinceVersionSource { get; set; }

    [JsonPropertyDescription("Equal to BranchName, but with / replaced with -.")]
    public string? EscapedBranchName { get; set; }

    [JsonPropertyDescription("The BuildMetaData suffixed with BranchName and Sha.")]
    public string? FullBuildMetaData { get; set; }

    [JsonPropertyDescription("The full, SemVer 2.0 compliant version number.")]
    public string? FullSemVer { get; set; }

    [JsonPropertyDescription("Suitable for .NET AssemblyInformationalVersion. Defaults to FullSemVer suffixed by FullBuildMetaData.")]
    public string? InformationalVersion { get; set; }

    [JsonPropertyDescription("A custom version, suitable for package managers, e.g. NuGet, with appropriate formatting. Defaults to SemVer.")]
    public string? CustomVersion { get; set; }

    [JsonPropertyDescription("The major version. Should be incremented on breaking changes.")]
    public int? Major { get; set; }

    [JsonPropertyDescription("Major, Minor and Patch joined together, separated by '.'.")]
    public string? MajorMinorPatch { get; set; }

    [JsonPropertyDescription("The minor version. Should be incremented on new features.")]
    public int? Minor { get; set; }

    [JsonPropertyDescription("The patch version. Should be incremented on bug fixes.")]
    public int? Patch { get; set; }

    [JsonPropertyDescription("The pre-release label is the name of the pre-release.")]
    public string? PreReleaseLabel { get; set; }

    [JsonPropertyDescription("The pre-release label prefixed with a dash.")]
    public string? PreReleaseLabelWithDash { get; set; }

    [JsonPropertyDescription("The pre-release number is the number of commits since the last version bump.")]
    public int? PreReleaseNumber { get; set; }

    [JsonPropertyDescription("The pre-release tag is the pre-release label suffixed by the PreReleaseNumber.")]
    public string? PreReleaseTag { get; set; }

    [JsonPropertyDescription("The pre-release tag prefixed with a dash.")]
    public string? PreReleaseTagWithDash { get; set; }

    [JsonPropertyDescription("The semantic version number, including PreReleaseTagWithDash for pre-release version numbers.")]
    public string? SemVer { get; set; }

    [JsonPropertyDescription("The SHA of the Git commit.")]
    public string? Sha { get; set; }

    [JsonPropertyDescription("The Sha limited to 7 characters.")]
    public string? ShortSha { get; set; }

    [JsonPropertyDescription("The number of uncommitted changes present in the repository.")]
    public int? UncommittedChanges { get; set; }

    [JsonPropertyDescription("The SHA of the commit used as version source.")]
    public string? VersionSourceSha { get; set; }

    [JsonPropertyDescription("A summation of branch specific pre-release-weight and the PreReleaseNumber. Can be used to obtain a monotonically increasing version number across the branches.")]
    public int? WeightedPreReleaseNumber { get; set; }
}
