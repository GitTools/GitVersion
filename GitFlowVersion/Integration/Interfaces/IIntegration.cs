namespace GitFlowVersion.Integration
{
    using System.Collections.Generic;

    public interface IIntegration
    {
        bool IsRunningInBuildAgent();
        IEnumerable<string> GenerateBuildLogOutput(VersionAndBranch versionAndBranch);
    }
}
