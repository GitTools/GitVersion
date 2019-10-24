namespace GitVersion.Configuration
{
    public class ConfigurationConstants
    {
        public const string DefaultTagPrefix = "[vV]";
        public const string ReleaseBranchRegex = "^releases?[/-]";
        public const string FeatureBranchRegex = "^features?[/-]";
        public const string PullRequestRegex = @"^(pull|pull\-requests|pr)[/-]";
        public const string HotfixBranchRegex = "^hotfix(es)?[/-]";
        public const string SupportBranchRegex = "^support[/-]";
        public const string DevelopBranchRegex = "^dev(elop)?(ment)?$";
        public const string MasterBranchRegex = "^master$";
        public const string MasterBranchKey = "master";
        public const string ReleaseBranchKey = "release";
        public const string FeatureBranchKey = "feature";
        public const string PullRequestBranchKey = "pull-request";
        public const string HotfixBranchKey = "hotfix";
        public const string SupportBranchKey = "support";
        public const string DevelopBranchKey = "develop";
    }
}
