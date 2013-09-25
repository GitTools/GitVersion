namespace GitFlowVersion
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    
    public class TeamCity
    {
        public static string GenerateBuildVersion(VersionAndBranch versionAndBranch)
        {
            var versionString = CreateVersionString(versionAndBranch);

            return string.Format("##teamcity[buildNumber '{0}']", versionString);
        }

        static string CreateVersionString(VersionAndBranch versionAndBranch, bool padPreReleaseNumber = false)
        {
            var prereleaseString = "";

            if (versionAndBranch.Version.Stability != Stability.Final)
            {
                var preReleaseNumber = versionAndBranch.Version.PreReleaseNumber.ToString();

                if (padPreReleaseNumber)
                {
                    preReleaseNumber = versionAndBranch.Version.PreReleaseNumber.Value.ToString("D4");
                }

                switch (versionAndBranch.BranchType)
                {
                    case BranchType.Develop:
                        prereleaseString = "-" + versionAndBranch.Version.Stability + preReleaseNumber;
                        break;

                    case BranchType.Release:
                        prereleaseString = "-" + versionAndBranch.Version.Stability + preReleaseNumber;
                        break;

                    case BranchType.Hotfix:
                        prereleaseString = "-" + versionAndBranch.Version.Stability + preReleaseNumber;
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


        public static bool IsRunningInBuildAgent()
        {
            var isRunningInBuildAgent = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));
            if (isRunningInBuildAgent)
            {
                Logger.Write("Executing inside a TeamCity build agent");
            }
            return isRunningInBuildAgent;
        }

        public static bool IsBuildingAPullRequest()
        {
            var branchInfo = GetBranchEnvironmentVariable();
            var isBuildingAPullRequest = !string.IsNullOrEmpty(branchInfo) && branchInfo.Contains("/Pull/");
            if (isBuildingAPullRequest)
            {
                Logger.Write("This is a pull request build for pull: " + CurrentPullRequestNo());
            }
            return isBuildingAPullRequest;
        }


        static string GetBranchEnvironmentVariable()
        {
            foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
            {
                if (((string) de.Key).StartsWith("teamcity.build.vcs.branch."))
                {
                    return (string) de.Value;
                }
            }

            return null;
        }

        public static int CurrentPullRequestNo()
        {
            return int.Parse(GetBranchEnvironmentVariable().Split('/')[2]);
        }

        public static IEnumerable<string> GenerateBuildLogOutput(VersionAndBranch versionAndBranch)
        {
            if (!IsRunningInBuildAgent())
            {
                yield break;
            }
            var semanticVersion = versionAndBranch.Version;

            yield return GenerateBuildVersion(versionAndBranch);
            yield return GenerateBuildParameter("Major", semanticVersion.Major.ToString());
            yield return GenerateBuildParameter("Minor", semanticVersion.Minor.ToString());
            yield return GenerateBuildParameter("Patch", semanticVersion.Patch.ToString());
            yield return GenerateBuildParameter("Stability", semanticVersion.Stability.ToString());
            yield return GenerateBuildParameter("PreReleaseNumber", semanticVersion.PreReleaseNumber.ToString());
            yield return GenerateBuildParameter("Version", CreateVersionString(versionAndBranch));
            yield return GenerateBuildParameter("NugetVersion", GenerateNugetVersion(versionAndBranch));
        }

        public static string GenerateNugetVersion(VersionAndBranch versionAndBranch)
        {
            return CreateVersionString(versionAndBranch, true);
        }

        static string GenerateBuildParameter(string name, string value)
        {
            return string.Format("##teamcity[setParameter name='GitFlowVersion.{0}' value='{1}']", name, value);
        }
    }

}