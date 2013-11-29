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
            string prefix = GitFlowVersionEnvironment.ContinuaCiVariablePrefix;
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                prefix += ".";
            }
            else
            {
                prefix = string.Empty;
            }

            return string.Format("@@continua[setVariable name='GitFlowVersion.{0}{1}' value='{2}']", prefix, name, value);
        }
    }
}
