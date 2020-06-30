using System;
using GitVersion.OutputVariables;

namespace GitVersion
{
    public interface IBuildAgent
    {
        bool CanApplyToCurrentContext();
        void WriteIntegration(Action<string> writer, VersionVariables variables, bool updateBuildNumber = true);
        string GetCurrentBranch(bool usingDynamicRepos);
        bool PreventFetch();
        bool ShouldCleanUpRemotes();
    }

    public interface ICurrentBuildAgent : IBuildAgent { }
}
