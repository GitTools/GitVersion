namespace GitFlowVersion
{
    using System.Linq;

    public class MergeMessageParser
    {
        public static bool TryParse(string message, out string semanticVersion)
        {
            string trimmed;
            if (message.StartsWith("Merge branch 'hotfix-"))
            {
                 trimmed = message.Replace("Merge branch 'hotfix-", "");
            }
            else if (message.StartsWith("Merge branch 'release-"))
            {
                trimmed = message.Replace("Merge branch 'release-", "");
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
            var versionPart = trimmed.Split('\'').First();
            semanticVersion = versionPart;
            return true;
        }
    }
}