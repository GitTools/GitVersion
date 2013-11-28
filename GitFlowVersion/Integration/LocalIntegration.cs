namespace GitFlowVersion.Integration
{
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;

    internal class LocalIntegration : IIntegration
    {
        public bool CanApplyToCurrentContext()
        {
            return true;
        }

        public AnalysisResult PerformPreProcessingSteps(ILogger logger, string gitDirectory)
        {
            logger.LogInfo("Executing in local mode.");

            if (string.IsNullOrEmpty(gitDirectory))
            {
                const string message =
                    "No .git directory found in provided solution path. This means the assembly may not be versioned correctly. " +
                    "To fix this warning either clone the repository using git or remove the `GitFlowVersion.Fody` nuget package. " +
                    "To temporarily work around this issue add a AssemblyInfo.cs with an appropriate `AssemblyVersionAttribute`.";
                logger.LogWarning(message);

                return AnalysisResult.EarlySuccessfulExit;
            }

            return AnalysisResult.Ok;
        }

        public IEnumerable<string> GenerateBuildLogOutput(VersionAndBranch versionAndBranch)
        {
            return Enumerable.Empty<string>();
        }
    }
}
