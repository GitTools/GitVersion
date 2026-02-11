namespace Common.Addins.GitVersion;

/// <summary>
/// GitVersion information.
/// </summary>
public sealed class GitVersion
{
    /// <summary>
    /// Gets or sets the assembly semantic file version. Suitable for .NET AssemblyFileVersion. Defaults to Major.Minor.Patch.0.
    /// </summary>
    public string? AssemblySemFileVer { get; set; }

    /// <summary>
    /// Gets or sets the assembly Semantic Version. Suitable for .NET AssemblyVersion. Defaults to Major.Minor.0.0.
    /// </summary>
    public string? AssemblySemVer { get; set; }

    /// <summary>
    /// Gets or sets the branch name. The name of the checked out Git branch.
    /// </summary>
    public string? BranchName { get; set; }

    /// <summary>
    /// Gets or sets the build metadata, usually representing number of commits since the VersionSourceSha.
    /// </summary>
    public int? BuildMetaData { get; set; }

    /// <summary>
    /// Gets or sets the commit date. The ISO-8601 formatted date of the commit identified by Sha.
    /// </summary>
    public string? CommitDate { get; set; }

    /// <summary>
    /// Gets or sets the commits since version source.
    /// </summary>
    [Obsolete("CommitsSinceVersionSource has been deprecated. Use VersionSourceDistance instead.")]
    public int? CommitsSinceVersionSource { get; set; }

    /// <summary>
    /// Gets or sets the escaped branch name. Equal to BranchName, but with / replaced with -.
    /// </summary>
    public string? EscapedBranchName { get; set; }

    /// <summary>
    /// Gets or sets the full build metadata. The BuildMetaData suffixed with BranchName and Sha.
    /// </summary>
    public string? FullBuildMetaData { get; set; }

    /// <summary>
    /// Gets or sets the full Semantic Version. The full, SemVer 2.0 compliant version number.
    /// </summary>
    public string? FullSemVer { get; set; }

    /// <summary>
    /// Gets or sets the informational version. Suitable for .NET AssemblyInformationalVersion. Defaults to FullSemVer suffixed by FullBuildMetaData.
    /// </summary>
    public string? InformationalVersion { get; set; }

    /// <summary>
    /// Gets or sets the major version. Should be incremented on breaking changes.
    /// </summary>
    public int? Major { get; set; }

    /// <summary>
    /// Gets or sets the major, minor, and patch. Major, Minor and Patch joined together, separated by '.'.
    /// </summary>
    public string? MajorMinorPatch { get; set; }

    /// <summary>
    /// Gets or sets the minor version. Should be incremented on new features.
    /// </summary>
    public int? Minor { get; set; }

    /// <summary>
    /// Gets or sets the patch version. Should be incremented on bug fixes.
    /// </summary>
    public int? Patch { get; set; }

    /// <summary>
    /// Gets or sets the pre-release label. The pre-release label is the name of the pre-release.
    /// </summary>
    public string? PreReleaseLabel { get; set; }

    /// <summary>
    /// Gets or sets the pre-release label with dash. The pre-release label prefixed with a dash.
    /// </summary>
    public string? PreReleaseLabelWithDash { get; set; }

    /// <summary>
    /// Gets or sets the pre-release number. The pre-release number is the number of commits since the last version bump.
    /// </summary>
    public int? PreReleaseNumber { get; set; }

    /// <summary>
    /// Gets or sets the pre-release tag. The pre-release tag is the pre-release label suffixed by the PreReleaseNumber.
    /// </summary>
    public string? PreReleaseTag { get; set; }

    /// <summary>
    /// Gets or sets the pre-release tag with dash. The pre-release tag prefixed with a dash.
    /// </summary>
    public string? PreReleaseTagWithDash { get; set; }

    /// <summary>
    /// Gets or sets the Semantic Version. The semantic version number, including PreReleaseTagWithDash for pre-release version numbers.
    /// </summary>
    public string? SemVer { get; set; }

    /// <summary>
    /// Gets or sets the Git SHA. The SHA of the Git commit.
    /// </summary>
    public string? Sha { get; set; }

    /// <summary>
    /// Gets or sets the short SHA. The Sha limited to 7 characters.
    /// </summary>
    public string? ShortSha { get; set; }

    /// <summary>
    /// Gets or sets the number of uncommitted changes present in the repository.
    /// </summary>
    public int? UncommittedChanges { get; set; }

    /// <summary>
    /// Gets or sets the version source distance. The number of commits since the version source.
    /// </summary>
    public int? VersionSourceDistance { get; set; }

    /// <summary>
    /// Gets or sets the version source SemVer. The semantic version of the commit used as version source.
    /// </summary>
    public string? VersionSourceSemVer { get; set; }

    /// <summary>
    /// Gets or sets the version source SHA. The SHA of the commit used as version source.
    /// </summary>
    public string? VersionSourceSha { get; set; }

    /// <summary>
    /// Gets or sets the weighted pre-release number. A summation of branch specific pre-release-weight and the PreReleaseNumber. Can be used to obtain a monotonically increasing version number across the branches.
    /// </summary>
    public int? WeightedPreReleaseNumber { get; set; }
}
