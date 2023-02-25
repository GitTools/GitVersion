using System.Globalization;
using GitVersion.Extensions;

namespace GitVersion.Configuration;

public class GitVersionConfiguration : BranchConfiguration
{
    private string? nextVersion;

    public GitVersionConfiguration()
    {
        Branches = new Dictionary<string, BranchConfiguration>();
        Ignore = new IgnoreConfiguration();
    }

    [JsonPropertyName("assembly-versioning-scheme")]
    public AssemblyVersioningScheme? AssemblyVersioningScheme { get; set; }

    [JsonPropertyName("assembly-file-versioning-scheme")]
    public AssemblyFileVersioningScheme? AssemblyFileVersioningScheme { get; set; }

    [JsonPropertyName("assembly-informational-format")]
    public string? AssemblyInformationalFormat { get; set; }

    [JsonPropertyName("assembly-versioning-format")]
    public string? AssemblyVersioningFormat { get; set; }

    [JsonPropertyName("assembly-file-versioning-format")]
    public string? AssemblyFileVersioningFormat { get; set; }

    [JsonPropertyName("label-prefix")]
    public string? LabelPrefix { get; set; }

    [JsonPropertyName("next-version")]
    public string? NextVersion
    {
        get => nextVersion;
        set =>
            nextVersion = int.TryParse(value, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var major)
                ? $"{major}.0"
                : value;
    }

    [JsonPropertyName("major-version-bump-message")]
    public string? MajorVersionBumpMessage { get; set; }

    [JsonPropertyName("minor-version-bump-message")]
    public string? MinorVersionBumpMessage { get; set; }

    [JsonPropertyName("patch-version-bump-message")]
    public string? PatchVersionBumpMessage { get; set; }

    [JsonPropertyName("no-bump-message")]
    public string? NoBumpMessage { get; set; }

    [JsonPropertyName("label-pre-release-weight")]
    public int? LabelPreReleaseWeight { get; set; }

    [JsonPropertyName("commit-date-format")]
    public string? CommitDateFormat { get; set; }

    [JsonPropertyName("merge-message-formats")]
    public Dictionary<string, string> MergeMessageFormats { get; set; } = new();

    [JsonPropertyName("update-build-number")]
    public bool UpdateBuildNumber { get; set; } = true;

    [JsonPropertyName("semantic-version-format")]
    public SemanticVersionFormat SemanticVersionFormat { get; set; }

    [JsonPropertyName("branches")]
    public Dictionary<string, BranchConfiguration> Branches { get; set; }

    [JsonPropertyName("ignore")]
    public IgnoreConfiguration Ignore { get; set; }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        using var stream = new StringWriter(stringBuilder);
        ConfigurationSerializer.Write(this, stream);
        stream.Flush();
        return stringBuilder.ToString();
    }

}
