namespace GitVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class MergeMessageParser
    {
        public static bool TryParse(Commit mergeCommit, out string versionPart)
        {
            versionPart = null;

            if (mergeCommit.Parents.Count() < 2)
            {
                return false;
            }

            var message = mergeCommit.Message;

            string trimmed;
            if (message.StartsWith("Merge branch 'hotfix-"))
            {
                trimmed = message.Replace("Merge branch 'hotfix-", "");
            }
            else if (message.StartsWith("Merge branch 'hotfix/"))
            {
                trimmed = message.Replace("Merge branch 'hotfix/", "");
            }
            else if (message.StartsWith("Merge branch 'release-"))
            {
                trimmed = message.Replace("Merge branch 'release-", "");
            }
            else if (message.StartsWith("Merge branch 'release/"))
            {
                trimmed = message.Replace("Merge branch 'release/", "");
            }
            else if (message.StartsWith("Merge branch '"))
            {
                trimmed = message.Replace("Merge branch '", "");
                if (!char.IsNumber(trimmed.First()))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            trimmed = trimmed.TrimToFirstLine();
            if (!trimmed.EndsWith("'"))
            {
                return false;
            }
            versionPart = trimmed.TrimEnd('\'');
            return true;
        }

    }
}