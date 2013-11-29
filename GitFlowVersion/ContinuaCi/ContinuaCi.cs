namespace GitFlowVersion
{
    using GitFlowVersion.Integration;

    public class ContinuaCi : IntegrationBase
    {
        public override bool CanApplyToCurrentContext()
        {
            return false;
        }

        public override AnalysisResult PerformPreProcessingSteps(ILogger logger, string gitDirectory)
        {
            logger.LogInfo("Executing inside a ContinuaCiVersionBuilder build agent");

            if (string.IsNullOrEmpty(gitDirectory))
            {
                // ReSharper disable once StringLiteralTypo
                logger.LogError("Failed to find .git directory on agent");
                return AnalysisResult.FatalError;
            }

            GitHelper.NormalizeGitDirectory(gitDirectory);

            return AnalysisResult.Ok;
        }

        protected override string GenerateBuildParameter(string name, string value)
        {
            // TODO: Read prefix from command line
            return string.Format("@@continua[setVariable name='GitFlowVersion.{0}' value='{1}']", name, value);
        }
    }
}
