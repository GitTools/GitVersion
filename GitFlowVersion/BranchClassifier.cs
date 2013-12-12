namespace GitFlowVersion
{
    using System;
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

        public static string GetSuffix(this Branch branch, BranchType branchType)
        {
            switch (branchType)
            {
                case BranchType.Hotfix:
                    return branch.GetHotfixSuffix();

                case BranchType.Release:
                    return branch.GetReleaseSuffix();

                default:
                    throw new NotSupportedException(string.Format("Unexpected branch type {0}.", branchType));
            }
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
            return branch.CanonicalName.Contains("/pull/");
        }
    }
}
