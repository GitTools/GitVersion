namespace GitFlowVersion
{
    using LibGit2Sharp;

    public static class BranchClassifier
    {

        public static bool IsHotfix(this Branch branch)
        {
            return branch.Name.StartsWith("hotfix-");
        }

        public static bool IsRelease(this Branch branch)
        {
            return branch.Name.StartsWith("release-");
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
            return branch.Name.StartsWith("feature-");
        }

        public static bool IsPullRequest(this Branch branch)
        {
            return branch.CanonicalName.Contains("/pull/") || TeamCity.IsBuildingAPullRequest();
        }
    }
}