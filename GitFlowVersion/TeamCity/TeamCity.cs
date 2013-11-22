namespace GitFlowVersion
{
    using System;
    using System.Collections;
    using GitFlowVersion.Integration;

    public class TeamCity : IntegrationBase
    {
        public override bool IsRunningInBuildAgent()
        {
            var isRunningInBuildAgent = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));
            if (isRunningInBuildAgent)
            {
                Logger.WriteInfo("Executing inside a TeamCityVersionBuilder build agent");
            }
            return isRunningInBuildAgent;
        }

        public override bool IsBuildingPullRequest()
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

        protected override string GenerateBuildParameter(string name, string value)
        {
            return string.Format("##teamcity[setParameter name='GitFlowVersion.{0}' value='{1}']", name, value);
        }

        public override int CurrentPullRequestNo()
        {
            return int.Parse(GetBranchEnvironmentVariable().Split('/')[2]);
        }
    }
}