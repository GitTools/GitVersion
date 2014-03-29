namespace GitVersion
{
    using System;
    using System.Text.RegularExpressions;

    public static class ReleaseInformationCalculator
    {
        public static ReleaseInformation CalculateReleaseInfo(this VersionAndBranch versionAndBranch)
        {
            return Calculate(versionAndBranch.BranchType, versionAndBranch.Version.Tag);
        }

        public static ReleaseInformation Calculate(BranchType? branchType, SemanticVersionTag tag)
        {
            return new ReleaseInformation(InferStability(tag), ReleaseNumber(tag));
        }

        private static int? ReleaseNumber(string tagName)
        {
            if (tagName == null || !tagName.Contains("-"))
            {
                return null;
            }
            
            int releaseNumber;
            var value = Regex.Match(tagName, "\\d+$").Value;
            if (int.TryParse(value, out releaseNumber))
                return releaseNumber;

            return null;
        }

        private static Stability? InferStability(string tagName)
        {
            if (tagName == null || !tagName.Contains("-"))
            {
                return Stability.Final;
            }
               
            var stageString = tagName.Substring(tagName.IndexOf("-")+1).TrimEnd("0123456789".ToCharArray());

            if (stageString.Equals("RC", StringComparison.InvariantCultureIgnoreCase))
            {
                return Stability.ReleaseCandidate;
            }

            if (stageString.Equals("hotfix", StringComparison.InvariantCultureIgnoreCase))
            {
                return Stability.Beta;
            }

            Stability stability;
            if (!Enum.TryParse(stageString, true, out stability))
            {
                return null;
            }

            return stability;
        }
    }
}