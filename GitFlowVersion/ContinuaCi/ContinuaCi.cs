namespace GitFlowVersion.Integration.ContinuaCI
{
    public class ContinuaCi : IntegrationBase
    {
        public override bool IsRunningInBuildAgent()
        {
            return false;
        }

        public override bool IsBuildingPullRequest()
        {
            return false;
        }

        public override int CurrentPullRequestNo()
        {
            return 0;
        }

        protected override string GenerateBuildParameter(string name, string value)
        {
            // TODO: Read prefix from command line
            return string.Format("##teamcity[setParameter name='GitFlowVersion.{0}' value='{1}']", name, value);
        }
    }
}
