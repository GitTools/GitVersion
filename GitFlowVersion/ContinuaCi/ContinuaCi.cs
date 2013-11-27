namespace GitFlowVersion.Integration.ContinuaCI
{
    public class ContinuaCi : IntegrationBase
    {
        public override bool IsRunningInBuildAgent()
        {
            return false;
        }

        protected override string GenerateBuildParameter(string name, string value)
        {
            // TODO: Read prefix from command line
            return string.Format("@@continua[setVariable name='GitFlowVersion.{0}' value='{1}']", name, value);
        }
    }
}
