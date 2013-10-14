namespace GitFlowVersion
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public static class TeamCity
    {

        public static bool IsRunningInBuildAgent()
        {
            var isRunningInBuildAgent = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));
            if (isRunningInBuildAgent)
            {
                Logger.WriteInfo("Executing inside a TeamCityVersionBuilder build agent");
            }
            return isRunningInBuildAgent;
        }

        public static bool IsBuildingAPullRequest()
        {
            var branchInfo = GetBranchEnvironmentVariable();
            var isBuildingAPullRequest = !string.IsNullOrEmpty(branchInfo) && branchInfo.Contains("/Pull/");
            if (isBuildingAPullRequest)
            {
                Logger.WriteInfo("This is a pull request build for pull: " + CurrentPullRequestNo());
            }
            return isBuildingAPullRequest;
        }


        static string GetBranchEnvironmentVariable()
        {
            foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
            {
                if (((string)de.Key).StartsWith("teamcity.build.vcs.branch."))
                {
                    return (string)de.Value;
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

            yield return TeamCityVersionBuilder.GenerateBuildVersion(versionAndBranch);
            yield return GenerateBuildParameter("Major", semanticVersion.Major.ToString());
            yield return GenerateBuildParameter("Minor", semanticVersion.Minor.ToString());
            yield return GenerateBuildParameter("Patch", semanticVersion.Patch.ToString());
            yield return GenerateBuildParameter("Stability", semanticVersion.Stability.ToString());
            yield return GenerateBuildParameter("PreReleaseNumber", semanticVersion.PreReleasePartOne.ToString());
            yield return GenerateBuildParameter("Version", TeamCityVersionBuilder.CreateVersionString(versionAndBranch));
            yield return GenerateBuildParameter("NugetVersion", NugetVersionBuilder.GenerateNugetVersion(versionAndBranch));
        }

        private static string GenerateBuildParameter(string name, string value)
        {
            return string.Format("##teamcity[setParameter name='GitFlowVersion.{0}' value='{1}']", name, value);
        }
    }
}