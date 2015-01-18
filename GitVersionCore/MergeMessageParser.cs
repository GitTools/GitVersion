namespace GitVersion
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    static class MergeMessageParser
    {
        public static bool TryParse(Commit mergeCommit, EffectiveConfiguration configuration, out SemanticVersion semanticVersion)
        {
            string versionPart;
            if (Inner(mergeCommit, out versionPart))
            {
                return SemanticVersion.TryParse(versionPart, configuration.GitTagPrefix, out semanticVersion);
            }
            semanticVersion = null;
            return false;
        }

        static bool Inner(Commit mergeCommit, out string versionPart)
        {
            if (mergeCommit.Parents.Count() < 2)
            {
                versionPart = null;
                return false;
            }

            var message = mergeCommit.Message.TrimToFirstLine();


            if (message.StartsWith("Merge branch 'hotfix-"))
            {
                var suffix = message.Replace("Merge branch 'hotfix-", "");
                return suffix.TryGetPrefix(out versionPart, "'");
            }

            if (message.StartsWith("Merge branch 'hotfix/"))
            {
                var suffix = message.Replace("Merge branch 'hotfix/", "");
                return suffix.TryGetPrefix(out versionPart, "'");
            }

            if (message.StartsWith("Merge branch 'release-"))
            {
                var suffix = message.Replace("Merge branch 'release-", "");
                return suffix.TryGetPrefix(out versionPart, "'");
            }

            if (message.StartsWith("Merge branch 'release/"))
            {
                var suffix = message.Replace("Merge branch 'release/", "");
                return suffix.TryGetPrefix(out versionPart, "'");
            }

            if (message.StartsWith("Merge branch '"))
            {
                var suffix = message.Replace("Merge branch '", "");

                if (suffix.Contains("-"))
                {
                    suffix = suffix.Split('-')[1];
                }
                return suffix.TryGetPrefix(out versionPart, "'");
            }

            if (message.StartsWith("Merge pull request #"))
            {
                var split = message.Split(new[]
                {
                    "/"
                }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length != 2)
                {
                    versionPart = null;
                    return false;
                }
                return split[1].TryGetSuffix(out versionPart, "-");
            }

            if (message.StartsWith("Finish Release-")) //Match Syntevo SmartGit client's GitFlow 'release' merge commit message formatting
            {
                versionPart = message.Replace("Finish Release-", "");
                return true;
            }

            if (message.StartsWith("Finish ")) //Match Syntevo SmartGit client's GitFlow 'hotfix' merge commit message formatting
            {
                versionPart = message.Replace("Finish ", "");
                return true;
            }

            versionPart = null;
            return false;
        }

        static bool TryGetPrefix(this string target, out string result, string splitter)
        {
            var indexOf = target.IndexOf(splitter);
            if (indexOf == -1)
            {
                result = null;
                return false;
            }
            result = target.Substring(0, indexOf);
            return true;
        }

        static bool TryGetSuffix(this string target, out string result, string splitter)
        {
            var indexOf = target.IndexOf(splitter);
            if (indexOf == -1)
            {
                result = null;
                return false;
            }
            result = target.Substring(indexOf + 1, target.Length - indexOf - 1);
            return true;
        }
    }
}