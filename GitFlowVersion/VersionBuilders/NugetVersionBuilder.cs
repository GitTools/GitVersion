namespace GitFlowVersion
{
    using System;
 
    public static class NugetVersionBuilder
    {
        public static string GenerateNugetVersion(this VersionAndBranch versionAndBranch)
        {
            var prereleaseString = "";

            var releaseInfo = versionAndBranch.CalculateReleaseInfo();
            if (!releaseInfo.Stability.HasValue)
            {
                throw new Exception("Stability cannot be null");
            }
            if (releaseInfo.Stability != Stability.Final)
            {
                var preReleaseVersion = releaseInfo.ReleaseNumber.Value.ToString("D4");
                if (versionAndBranch.Version.PreReleasePartTwo != null)
                {
                    preReleaseVersion += "-" + versionAndBranch.Version.PreReleasePartTwo.Value.ToString("D4");
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
                        prereleaseString = "-PullRequest-" + versionAndBranch.Version.Suffix;
                        break;
                    case BranchType.Feature:
                        prereleaseString = "-Feature-" + versionAndBranch.BranchName + "-" + versionAndBranch.Sha;
                        break;
                }
            }
            return string.Format("{0}.{1}.{2}{3}", versionAndBranch.Version.Major, versionAndBranch.Version.Minor, versionAndBranch.Version.Patch, prereleaseString);
        }

    }

}