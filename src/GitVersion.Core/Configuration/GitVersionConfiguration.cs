using System.Globalization;
using GitVersion.Extensions;
using YamlDotNet.Serialization;

namespace GitVersion.Configuration;

public class GitVersionConfiguration : BranchConfiguration
{
    private string? nextVersion;

    public GitVersionConfiguration()
    {
        Branches = new Dictionary<string, BranchConfiguration>();
        Ignore = new IgnoreConfiguration();
    }

    [YamlMember(Alias = "assembly-versioning-scheme")]
    public AssemblyVersioningScheme? AssemblyVersioningScheme { get; set; }

    [YamlMember(Alias = "assembly-file-versioning-scheme")]
    public AssemblyFileVersioningScheme? AssemblyFileVersioningScheme { get; set; }

    [YamlMember(Alias = "assembly-informational-format")]
    public string? AssemblyInformationalFormat { get; set; }

    [YamlMember(Alias = "assembly-versioning-format")]
    public string? AssemblyVersioningFormat { get; set; }

    [YamlMember(Alias = "assembly-file-versioning-format")]
    public string? AssemblyFileVersioningFormat { get; set; }

    [YamlMember(Alias = "label-prefix")]
    public string? LabelPrefix { get; set; }

    [YamlMember(Alias = "next-version")]
    public string? NextVersion
    {
        get => nextVersion;
        set =>
            nextVersion = int.TryParse(value, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var major)
                ? $"{major}.0"
                : value;
    }

    [YamlMember(Alias = "major-version-bump-message")]
    public string? MajorVersionBumpMessage { get; set; }

    [YamlMember(Alias = "minor-version-bump-message")]
    public string? MinorVersionBumpMessage { get; set; }

    [YamlMember(Alias = "patch-version-bump-message")]
    public string? PatchVersionBumpMessage { get; set; }

    [YamlMember(Alias = "no-bump-message")]
    public string? NoBumpMessage { get; set; }

    [YamlMember(Alias = "label-pre-release-weight")]
    public int? LabelPreReleaseWeight { get; set; }

    [YamlMember(Alias = "commit-date-format")]
    public string? CommitDateFormat { get; set; }

    [YamlMember(Alias = "merge-message-formats")]
    public Dictionary<string, string> MergeMessageFormats { get; set; } = new();

    [YamlMember(Alias = "update-build-number")]
    public bool UpdateBuildNumber { get; set; } = true;

    [YamlMember(Alias = "semantic-version-format")]
    public SemanticVersionFormat SemanticVersionFormat { get; set; }

    [YamlMember(Alias = "branches")]
    public Dictionary<string, BranchConfiguration> Branches { get; set; }

    [YamlMember(Alias = "ignore")]
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
