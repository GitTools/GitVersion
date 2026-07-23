using Microsoft.Build.Framework;

namespace GitVersion.MsBuild.Tasks;

public class GetVersion : GitVersionTaskBase
{
    [Output]
    public string AssemblySemFileVer { get; set; } = null!;

    [Output]
    public string AssemblySemVer { get; set; } = null!;

    [Output]
    public string BranchName { get; set; } = null!;

    [Output]
    public string BuildMetaData { get; set; } = null!;

    [Output]
    public string CommitDate { get; set; } = null!;

    [Output]
    public string EscapedBranchName { get; set; } = null!;

    [Output]
    public string FullBuildMetaData { get; set; } = null!;

    [Output]
    public string FullSemVer { get; set; } = null!;

    [Output]
    public string InformationalVersion { get; set; } = null!;

    [Output]
    public string Major { get; set; } = null!;

    [Output]
    public string MajorMinorPatch { get; set; } = null!;

    [Output]
    public string Minor { get; set; } = null!;

    [Output]
    public string Patch { get; set; } = null!;

    [Output]
    public string PreReleaseLabel { get; set; } = null!;

    [Output]
    public string PreReleaseLabelWithDash { get; set; } = null!;

    [Output]
    public string PreReleaseNumber { get; set; } = null!;

    [Output]
    public string PreReleaseTag { get; set; } = null!;

    [Output]
    public string PreReleaseTagWithDash { get; set; } = null!;

    [Output]
    public string SemVer { get; set; } = null!;

    [Output]
    public string Sha { get; set; } = null!;

    [Output]
    public string ShortSha { get; set; } = null!;

    [Output]
    public string UncommittedChanges { get; set; } = null!;

    [Output]
    public string VersionSourceDistance { get; set; } = null!;

    [Output]
    public string VersionSourceIncrement { get; set; } = null!;

    [Output]
    public string VersionSourceSemVer { get; set; } = null!;

    [Output]
    public string VersionSourceSha { get; set; } = null!;

    [Output]
    public string WeightedPreReleaseNumber { get; set; } = null!;
}
