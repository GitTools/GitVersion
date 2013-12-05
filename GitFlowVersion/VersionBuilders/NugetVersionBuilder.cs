namespace GitFlowVersion
{
    using System;
 
    public static class NugetVersionBuilder
    {

        public static string GenerateNugetVersion(this VersionAndBranch versionAndBranch)
        {
            var prereleaseString = "";

            var stability = versionAndBranch.Version.Stability;
            if (stability == null)
            {
                throw new Exception("Stability cannot be null");
            }
            if (stability != Stability.Final)
            {
                var preReleaseVersion = versionAndBranch.Version.PreReleasePartOne.Value.ToString("D4");
                if (versionAndBranch.Version.PreReleasePartTwo != null)
                {
                    preReleaseVersion += "-" + versionAndBranch.Version.PreReleasePartTwo.Value.ToString("D4");
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