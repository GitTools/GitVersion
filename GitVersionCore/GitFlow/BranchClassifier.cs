namespace GitVersion
{
    using LibGit2Sharp;

    static class BranchClassifier
    {

        public static bool IsHotfix(this Branch branch)
        {
            return branch.Name.StartsWith("hotfix-") || branch.Name.StartsWith("hotfix/");
        }

        public static bool IsRelease(this Branch branch)
        {
            return branch.Name.StartsWith("release-") || branch.Name.StartsWith("release/");
        }

        public static bool IsDevelop(this Branch branch)
        {
            return branch.Name == "develop";
        }

        public static bool IsMaster(this Branch branch)
        {
            return branch.Name == "master";
        }

        public static bool IsPullRequest(this Branch branch)
        {
            return branch.CanonicalName.Contains("/pull/") || branch.CanonicalName.Contains("/pull-requests/");
        }

        public static bool IsSupport(this Branch branch)
        {
            return branch.Name.ToLower().StartsWith("support-");
        }
    }
}
