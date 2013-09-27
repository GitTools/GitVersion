namespace GitFlowVersion
{
    using System;
    using System.Linq;

    public class MergeMessageParser
    {

        public static string GetVersionFromMergeCommit(string message)
        {
            var array = message
                .SkipWhile(x => !char.IsNumber(x))
                .TakeWhile(x => x == '.' || Char.IsNumber(x))
                .ToArray();
            return new string(array);
        }
        public static bool TryParse(string message, out SemanticVersion semanticVersion)
        {
            string trimmed;
            if (message.StartsWith("Merge branch 'hotfix-"))
            {
                 trimmed = message.Replace("Merge branch 'hotfix-", "");
            }
            else if (message.StartsWith("Merge branch 'release-"))
            {
                 trimmed = message.Replace("Merge branch 'hotfix-", "");
            }
            else if (message.StartsWith("Merge branch '"))
            {
                trimmed = message.Replace("Merge branch '", "");
            }
            else
            {
                semanticVersion = null;
                return false;
            }
            var versionPart = GetVersionFromMergeCommit(trimmed);
            if (SemanticVersionParser.IsVersion(versionPart))
            {
                semanticVersion = SemanticVersionParser.FromMajorMinorPatch(versionPart);
                return true;
            }
            semanticVersion = null;
            return false;
        }
    }
}