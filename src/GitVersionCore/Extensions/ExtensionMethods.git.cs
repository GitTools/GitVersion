namespace GitVersion
{
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

        public static bool IsPullRequest(this string branchName)
        {
            return branchName.Contains("pull/") || branchName.Contains("pull-requests/") || branchName.Contains("pr/");
        }
    }
}
