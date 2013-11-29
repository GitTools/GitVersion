namespace GitFlowVersion.Integration
{
    using System.Collections.Generic;

    public abstract class IntegrationBase : IIntegration
    {
        public abstract bool CanApplyToCurrentContext();
        public abstract AnalysisResult PerformPreProcessingSteps(ILogger logger, string gitDirectory);

        public virtual IEnumerable<string> GenerateBuildLogOutput(VersionAndBranch versionAndBranch)
        {
            if (!CanApplyToCurrentContext())
            {
                return new string[] { };
            }

            return IntegrationHelper.GenerateBuildLogOutput(versionAndBranch, new TeamCityVersionBuilder(), GenerateBuildParameter);
        }

        protected abstract string GenerateBuildParameter(string name, string value);
    }
}
