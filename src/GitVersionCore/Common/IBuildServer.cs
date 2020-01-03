using System;
using GitVersion.OutputVariables;

namespace GitVersion
{
    public interface IBuildServer
    {
        bool CanApplyToCurrentContext();
        string GenerateSetVersionMessage(VersionVariables variables);
        string[] GenerateSetParameterMessage(string name, string value);
        void WriteIntegration(Action<string> writer, VersionVariables variables);
        string GetCurrentBranch(bool usingDynamicRepos);
        /// <summary>
        /// If the build server should not try and fetch
        /// </summary>
        bool PreventFetch();
        bool ShouldCleanUpRemotes();
    }
}
