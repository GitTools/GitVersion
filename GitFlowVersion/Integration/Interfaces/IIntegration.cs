namespace GitFlowVersion.Integration
{
    using System.Collections.Generic;
    using Interfaces;

    public interface IIntegration
    {
        bool CanApplyToCurrentContext();
        AnalysisResult PerformPreProcessingSteps(ILogger logger, string gitDirectory);
        IEnumerable<string> GenerateBuildLogOutput(VersionAndBranch versionAndBranch);
    }
}
