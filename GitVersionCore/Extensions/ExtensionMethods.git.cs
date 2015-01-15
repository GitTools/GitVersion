namespace GitVersion
{
    using System;

    static partial class ExtensionMethods
    {
        public static string GetCanonicalBranchName(this string branchName)
        {
            if (branchName.IsPullRequest())
            {
                branchName = branchName.Replace("pull-requests", "pull");
                branchName = branchName.Replace("pr", "pull");

                return string.Format("refs/{0}/head", branchName);
            }

            return string.Format("refs/heads/{0}", branchName);
        }

        public static bool IsHotfix(this string branchName)
        {
            return branchName.StartsWith("hotfix-") || branchName.StartsWith("hotfix/");
        }

        public static string GetHotfixSuffix(this string branchName)
        {
            return branchName.TrimStart("hotfix-").TrimStart("hotfix/");
        }

        public static bool IsRelease(this string branchName)
        {
            return branchName.StartsWith("release-") || branchName.StartsWith("release/");
        }

        public static string GetReleaseSuffix(this string branchName)
        {
            return branchName.TrimStart("release-").TrimStart("release/");
        }

        public static string GetUnknownBranchSuffix(this string branchName)
        {
            var unknownBranchSuffix = branchName.Split('-', '/');
            if (unknownBranchSuffix.Length == 1)
                return branchName;
            return unknownBranchSuffix[1];
        }

        public static string GetSuffix(this string branchName, BranchType branchType)
        {
            switch (branchType)
            {
                case BranchType.Hotfix:
                    return branchName.GetHotfixSuffix();

                case BranchType.Release:
                    return branchName.GetReleaseSuffix();

                case BranchType.Unknown:
                    return branchName.GetUnknownBranchSuffix();

                default:
                    throw new NotSupportedException(string.Format("Unexpected branch type {0}.", branchType));
            }
        }

        public static bool IsDevelop(this string branchName)
        {
            return branchName == "develop";
        }

        public static bool IsMaster(this string branchName)
        {
            return branchName == "master";
        }

        public static bool IsPullRequest(this string branchName)
        {
            return branchName.Contains("pull/") || branchName.Contains("pull-requests/") || branchName.Contains("pr/");
        }

        public static bool IsSupport(this string branchName)
        {
            return branchName.ToLower().StartsWith("support-") || branchName.ToLower().StartsWith("support/");
        }
    }
}
