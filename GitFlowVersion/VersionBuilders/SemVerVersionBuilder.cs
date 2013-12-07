namespace GitFlowVersion.VersionBuilders
{
    using System;

    public static class SemVerVersionBuilder
    {
        public static string GenerateSemVer(this VersionAndBranch versionAndBranch)
        {
            var prereleaseString = string.Empty;

            var semVer = versionAndBranch.Version;
            var stability = semVer.Stability;
            if (stability == null)
            {
                throw new Exception("Stability cannot be null");
            }
            if (stability != Stability.Final)
            {
                var preReleaseVersion = semVer.PreReleasePartOne.ToString();
                if (semVer.PreReleasePartTwo != null)
                {
                    preReleaseVersion += "." + semVer.PreReleasePartTwo;
                }

                switch (versionAndBranch.BranchType)
                {
                    case BranchType.Develop:
                        prereleaseString = "-" + stability + preReleaseVersion;
                        break;

                    case BranchType.Release:
                        prereleaseString = "-" + stability + preReleaseVersion;
                        break;

                    case BranchType.Hotfix:
                        prereleaseString = "-" + stability + preReleaseVersion;
                        break;
                    case BranchType.PullRequest:
                        prereleaseString = "-PullRequest-" + semVer.Suffix;
                        break;
                    case BranchType.Feature:
                        prereleaseString = "-Feature-" + versionAndBranch.BranchName + "-" + versionAndBranch.Sha;
                        break;
                }
            }
            return string.Format("{0}.{1}.{2}{3}", semVer.Major, semVer.Minor, semVer.Patch, prereleaseString);
        }
    }
}
