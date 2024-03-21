using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

internal static class ConfigurationConstants
{
    internal const string NameOfDefaultAssemblyVersioningScheme = nameof(AssemblyVersioningScheme.MajorMinorPatch);
    internal const string NameOfDefaultAssemblyFileVersioningScheme = nameof(AssemblyFileVersioningScheme.MajorMinorPatch);
    internal const string StringDefaultSemanticVersionFormat = nameof(SemanticVersionFormat.Strict);
    internal const string StringDefaultTagPreReleaseWeight = "60000";
    internal const string StringDefaultUpdateBuildNumber = "true";

    public const AssemblyVersioningScheme DefaultAssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch;
    public const AssemblyFileVersioningScheme DefaultAssemblyFileVersioningScheme = AssemblyFileVersioningScheme.MajorMinorPatch;
    public const SemanticVersionFormat DefaultSemanticVersionFormat = SemanticVersionFormat.Strict;
    public static readonly VersionStrategies[] DefaultVersionStrategies = [
        VersionStrategies.Fallback,
        VersionStrategies.ConfiguredNextVersion,
        VersionStrategies.MergeMessage,
        VersionStrategies.TaggedCommit,
        VersionStrategies.TrackReleaseBranches,
        VersionStrategies.VersionInBranchName
    ];
    public const string DefaultAssemblyInformationalFormat = "{InformationalVersion}";
    public const string DefaultTagPrefix = "[vV]?";
    public const string DefaultVersionInBranchPattern = @"(?<version>[vV]?\d+(\.\d+)?(\.\d+)?).*";
    public const string DefaultCommitDateFormat = "yyyy-MM-dd";
    public const string BranchNamePlaceholder = "{BranchName}";
    public const string DefaultLabelNumberPattern = @"[/-](?<number>\d+)";
    public const bool DefaultUpdateBuildNumber = true;
    public const int DefaultTagPreReleaseWeight = 60000;

    public const string MainBranchKey = "main";
    public const string MasterBranchKey = "master";
    public const string DevelopBranchKey = "develop";
    public const string ReleaseBranchKey = "release";
    public const string FeatureBranchKey = "feature";
    public const string PullRequestBranchKey = "pull-request";
    public const string HotfixBranchKey = "hotfix";
    public const string SupportBranchKey = "support";
    public const string UnknownBranchKey = "unknown";

    public const string MainBranchRegex = "^master$|^main$";
    public const string DevelopBranchRegex = "^dev(elop)?(ment)?$";
    public const string ReleaseBranchRegex = "^releases?[/-](?<BranchName>.+)";
    public const string FeatureBranchRegex = "^features?[/-](?<BranchName>.+)";
    public const string PullRequestBranchRegex = @"^(pull|pull\-requests|pr)[/-]";
    public const string HotfixBranchRegex = "^hotfix(es)?[/-](?<BranchName>.+)";
    public const string SupportBranchRegex = "^support[/-](?<BranchName>.+)";
    public const string UnknownBranchRegex = "(?<BranchName>.+)";
}
