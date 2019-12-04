using System;
using System.Collections.Generic;
using System.Text;

namespace GitVersion.BuildServers
{
    using GitVersion.OutputVariables;

    public class GitHubActions: IBuildServer
    {
        public bool CanApplyToCurrentContext()
        {
            throw new NotImplementedException();
        }

        public string GenerateSetVersionMessage(VersionVariables variables)
        {
            throw new NotImplementedException();
        }

        public string[] GenerateSetParameterMessage(string name, string value)
        {
            throw new NotImplementedException();
        }

        public void WriteIntegration(Action<string> writer, VersionVariables variables)
        {
            throw new NotImplementedException();
        }

        public string GetCurrentBranch(bool usingDynamicRepos)
        {
            throw new NotImplementedException();
        }

        public bool PreventFetch()
        {
            throw new NotImplementedException();
        }

        public bool ShouldCleanUpRemotes()
        {
            throw new NotImplementedException();
        }
    }
}
