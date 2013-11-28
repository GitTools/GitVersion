namespace GitFlowVersion.Integration.ContinuaCI
{
    using Interfaces;

    public class ContinuaCi : IntegrationBase
    {
        public override bool CanApplyToCurrentContext()
        {
            return false;
        }

        public override AnalysisResult PerformPreProcessingSteps(ILogger logger, string gitDirectory)
        {
            throw new System.NotImplementedException();
        }

        protected override string GenerateBuildParameter(string name, string value)
        {
            // TODO: Read prefix from command line
            return string.Format("@@continua[setVariable name='GitFlowVersion.{0}' value='{1}']", name, value);
        }
    }
}
