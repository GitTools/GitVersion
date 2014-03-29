namespace GitVersion
{
    public static class VersionInformationalBuilder
    {
        public static string ToLongString(this VersionAndBranch versionAndBranch)
        {
            var semVer = versionAndBranch.GenerateSemVer();
            var suffix = string.IsNullOrEmpty(versionAndBranch.Version.Suffix) ? null : versionAndBranch.Version.Suffix + ".";

            if (versionAndBranch.BranchType == BranchType.Master)
            {
                return string.Format("{0}+{1}Sha.{2}", semVer, suffix, versionAndBranch.Sha);
            }

            return string.Format("{0}+{1}Branch.{2}.Sha.{3}", semVer, suffix, versionAndBranch.BranchName, versionAndBranch.Sha);
        }
    }
}