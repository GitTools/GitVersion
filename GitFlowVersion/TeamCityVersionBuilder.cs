namespace GitFlowVersion
{
    using System;

    public class TeamCityVersionBuilder
    {
        public static string GenerateBuildVersion(VersionAndBranch versionAndBranch)
        {
            var versionString = CreateVersionString(versionAndBranch);

            return string.Format("##teamcity[buildNumber '{0}']", versionString);
        }

        public static string CreateVersionString(VersionAndBranch versionAndBranch)
        {
            var prereleaseString = "";

            var stability = versionAndBranch.Version.Stability;
            if (stability == null)
            {
                throw new Exception("Stability cannot be null");
            }
            if (stability != Stability.Final)
            {
                var preReleaseVersion = versionAndBranch.Version.PreReleasePartOne.ToString();
                if (versionAndBranch.Version.PreReleasePartTwo != null)
                {
                    preReleaseVersion += "." + versionAndBranch.Version.PreReleasePartTwo;
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
            return string.Format("{0}.{1}.{2}{3}", versionAndBranch.Version.Major, versionAndBranch.Version.Minor,
                versionAndBranch.Version.Patch, prereleaseString);
        }



        private static string GenerateBuildParameter(string name, string value)
        {
            return string.Format("##teamcity[setParameter name='GitFlowVersion.{0}' value='{1}']", name, value);
        }
    }

}