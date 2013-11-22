namespace GitFlowVersion.Integration
{
    using System.Collections.Generic;

    public interface IIntegration
    {
        bool IsRunningInBuildAgent();
        bool IsBuildingPullRequest();
        int CurrentPullRequestNo();
        IEnumerable<string> GenerateBuildLogOutput(VersionAndBranch versionAndBranch);
    }
}
