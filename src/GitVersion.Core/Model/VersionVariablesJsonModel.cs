using System.Text.Json.Serialization;

namespace GitVersion.OutputVariables;

public class VersionVariablesJsonModel
{
    [JsonConverter(typeof(VersionVariablesJsonNumberConverter))]
    public string? Major { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonNumberConverter))]
    public string? Minor { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonNumberConverter))]
    public string? Patch { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? PreReleaseTag { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? PreReleaseTagWithDash { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? PreReleaseLabel { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? PreReleaseLabelWithDash { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonNumberConverter))]
    public string? PreReleaseNumber { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonNumberConverter))]
    public string? WeightedPreReleaseNumber { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonNumberConverter))]
    public string? BuildMetaData { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? BuildMetaDataPadded { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? FullBuildMetaData { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? MajorMinorPatch { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? SemVer { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? LegacySemVer { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? LegacySemVerPadded { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? AssemblySemVer { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? AssemblySemFileVer { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? FullSemVer { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? InformationalVersion { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? BranchName { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? EscapedBranchName { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? Sha { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? ShortSha { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? NuGetVersionV2 { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? NuGetVersion { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? NuGetPreReleaseTagV2 { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? NuGetPreReleaseTag { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? VersionSourceSha { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonNumberConverter))]
    public string? CommitsSinceVersionSource { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? CommitsSinceVersionSourcePadded { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonNumberConverter))]
    public string? UncommittedChanges { get; set; }
    [JsonConverter(typeof(VersionVariablesJsonStringConverter))]
    public string? CommitDate { get; set; }
}
