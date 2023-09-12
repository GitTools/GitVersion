namespace GitVersion.Configuration;

internal static class ConfigurationConstants
{
    public const string DefaultTagPrefix = "[vV]?";
    public const string DefaultVersionInBranchPattern = @"(?<version>[vV]?\d+(\.\d+)?(\.\d+)?).*";
    public const string BranchNamePlaceholder = "{BranchName}";

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
    public const string ReleaseBranchRegex = "^releases?[/-]";
    public const string FeatureBranchRegex = "^features?[/-](?<BranchName>.+)";
    public const string PullRequestBranchRegex = @"^(pull|pull\-requests|pr)[/-]";
    public const string HotfixBranchRegex = "^hotfix(es)?[/-]";
    public const string SupportBranchRegex = "^support[/-]";
    public const string UnknownBranchRegex = "(?<BranchName>.*)";
}
