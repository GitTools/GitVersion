namespace GitFlowVersion.Integration
{
    using System.Collections.Generic;

    public abstract class IntegrationBase : IIntegration
    {
        public abstract bool IsRunningInBuildAgent();

        public virtual IEnumerable<string> GenerateBuildLogOutput(VersionAndBranch versionAndBranch)
        {
            if (!IsRunningInBuildAgent())
            {
                return new string[] { };
            }

            return IntegrationHelper.GenerateBuildLogOutput(versionAndBranch, new TeamCityVersionBuilder(), GenerateBuildParameter);
        }

        protected abstract string GenerateBuildParameter(string name, string value);
    }
}
