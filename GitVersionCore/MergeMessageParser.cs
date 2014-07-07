namespace GitVersion
{
    using System.Linq;
    using System.Text.RegularExpressions;
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
            else if (Regex.IsMatch(message, "Merge pull request #\\d+ from "))
            {
                var branch = Regex.Match(message, "from (?<branch>.*)").Groups["branch"].Value;
                var lastBranchPart = branch.Split('/', '-').Last();

                if (!char.IsNumber(lastBranchPart.First()) || !lastBranchPart.Contains("."))
                {
                    return false;
                }

                versionPart = lastBranchPart;
                return true;
            }
            else if (Regex.IsMatch(message, "Merge pull request #\\d+ in "))
            {
                var branch = Regex.Match(message, "in (?<branch>.*)").Groups["branch"].Value;
                var lastBranchPart = branch.Split('/', '-').Last();

                if (!char.IsNumber(lastBranchPart.First()) || !lastBranchPart.Contains("."))
                {
                    return false;
                }

                versionPart = lastBranchPart;
                return true;
            }
            else if (message.StartsWith("Merge branch '"))
            {
                trimmed = message.Replace("Merge branch '", "");
                var branchName = trimmed.Split('\'').First();
                var dashSeparared = branchName.Split('-');
                // Support branchname-1.2.3
                if (dashSeparared.Length == 2)
                {
                    trimmed = dashSeparared[1];
                    if (!char.IsNumber(trimmed.First()))
                        return false;

                    versionPart = trimmed;
                    return true;
                }
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