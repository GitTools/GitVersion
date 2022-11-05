using System.Globalization;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;
using YamlDotNet.Serialization;

namespace GitVersion.Configuration;

public class GitVersionConfiguration
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

    [YamlMember(Alias = "mode")]
    public VersioningMode? VersioningMode { get; set; }

    [YamlMember(Alias = "tag-prefix")]
    public string? TagPrefix { get; set; }

    [YamlMember(Alias = "continuous-delivery-fallback-tag")]
    public string? ContinuousDeploymentFallbackTag { get; set; }

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

    [YamlMember(Alias = "tag-pre-release-weight")]
    public int? TagPreReleaseWeight { get; set; }

    [YamlMember(Alias = "commit-message-incrementing")]
    public CommitMessageIncrementMode? CommitMessageIncrementing { get; set; }

    [YamlMember(Alias = "branches")]
    public Dictionary<string, BranchConfiguration> Branches { get; set; }

    [YamlMember(Alias = "ignore")]
    public IgnoreConfiguration Ignore { get; set; }

    [YamlMember(Alias = "increment")]
    public IncrementStrategy? Increment { get; set; }

    [YamlMember(Alias = "commit-date-format")]
    public string? CommitDateFormat { get; set; }

    [YamlMember(Alias = "merge-message-formats")]
    public Dictionary<string, string> MergeMessageFormats { get; set; } = new();

    [YamlMember(Alias = "update-build-number")]
    public bool? UpdateBuildNumber { get; set; }

    [YamlMember(Alias = "semver-format")]
    public SemanticVersionFormat SemanticVersionFormat { get; set; } = SemanticVersionFormat.Strict;

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        using var stream = new StringWriter(stringBuilder);
        ConfigurationSerializer.Write(this, stream);
        stream.Flush();
        return stringBuilder.ToString();
    }

    public const string DefaultTagPrefix = "[vV]?";
    public const string ReleaseBranchRegex = "^releases?[/-]";
    public const string FeatureBranchRegex = "^features?[/-]";
    public const string PullRequestRegex = @"^(pull|pull\-requests|pr)[/-]";
    public const string HotfixBranchRegex = "^hotfix(es)?[/-]";
    public const string SupportBranchRegex = "^support[/-]";
    public const string DevelopBranchRegex = "^dev(elop)?(ment)?$";
    public const string MainBranchRegex = "^master$|^main$";

    public const string MainBranchKey = "main";
    public const string MasterBranchKey = "master";
    public const string ReleaseBranchKey = "release";
    public const string FeatureBranchKey = "feature";
    public const string PullRequestBranchKey = "pull-request";
    public const string HotfixBranchKey = "hotfix";
    public const string SupportBranchKey = "support";
    public const string DevelopBranchKey = "develop";
}
