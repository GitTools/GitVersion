namespace GitVersion
{
    using System;

    public static class SemVerVersionBuilder
    {
        public static string GenerateSemVer(this VersionAndBranch versionAndBranch)
        {
            var prereleaseString = string.Empty;

            var semVer = versionAndBranch.Version;
            var releaseInfo = versionAndBranch.CalculateReleaseInfo();
            if (releaseInfo.Stability == null)
            {
                throw new Exception("Stability cannot be null");
            }
            if (releaseInfo.Stability != Stability.Final)
            {
                var preReleaseVersion = releaseInfo.ReleaseNumber.ToString();
                if (semVer.PreReleasePartTwo != null)
                {
                    preReleaseVersion += "." + semVer.PreReleasePartTwo;
                }

                switch (versionAndBranch.BranchType)
                {
                    case BranchType.Develop:
                        prereleaseString = "-" + releaseInfo.Stability + preReleaseVersion;
                        break;

                    case BranchType.Release:
                        prereleaseString = "-" + releaseInfo.Stability + preReleaseVersion;
                        break;

                    case BranchType.Hotfix:
                        prereleaseString = "-" + releaseInfo.Stability + preReleaseVersion;
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
