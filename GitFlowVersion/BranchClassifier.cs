namespace GitFlowVersion
{
    using LibGit2Sharp;

    static class BranchClassifier
    {
        public static bool IsHotfix(this Branch branch)
        {
            return branch.Name.StartsWith("hotfix-") || branch.Name.StartsWith("hotfix/");
        }

        public static string GetHotfixSuffix(this Branch branch)
        {
            return branch.Name.TrimStart("hotfix-").TrimStart("hotfix/");
        }

        public static bool IsRelease(this Branch branch)
        {
            return branch.Name.StartsWith("release-") || branch.Name.StartsWith("release/");
        }

        public static string GetReleaseSuffix(this Branch branch)
        {
            return branch.Name.TrimStart("release-").TrimStart("release/");
        }

        public static bool IsDevelop(this Branch branch)
        {
            return branch.Name == "develop";
        }

        public static bool IsMaster(this Branch branch)
        {
            return branch.Name == "master";
        }

        public static bool IsFeature(this Branch branch)
        {
            return branch.Name.StartsWith("feature-") || branch.Name.StartsWith("feature/");
        }

        public static bool IsPullRequest(this Branch branch)
        {
            return branch.CanonicalName.Contains("/pull/");
        }
    }
}