namespace GitVersion
{
    using LibGit2Sharp;

    static class BranchClassifier
    {
        public static bool IsHotfix(this Branch branch)
        {
            return branch.Name.IsHotfix();
        }

        public static string GetHotfixSuffix(this Branch branch)
        {
            return branch.Name.GetHotfixSuffix();
        }

        public static bool IsRelease(this Branch branch)
        {
            return branch.Name.IsRelease();
        }

        public static string GetReleaseSuffix(this Branch branch)
        {
            return branch.Name.GetReleaseSuffix();
        }

        public static string GetUnknownBranchSuffix(this Branch branch)
        {
            return branch.Name.GetUnknownBranchSuffix();
        }

        public static string GetSuffix(this Branch branch, BranchType branchType)
        {
            return branch.CanonicalName.GetSuffix(branchType);
        }

        public static bool IsDevelop(this Branch branch)
        {
            return branch.Name.IsDevelop();
        }

        public static bool IsMaster(this Branch branch)
        {
            return branch.Name.IsMaster();
        }

        public static bool IsPullRequest(this Branch branch)
        {
            return branch.CanonicalName.IsPullRequest();
        }

        public static bool IsSupport(this Branch branch)
        {
            return branch.Name.IsSupport();
        }
    }
}
