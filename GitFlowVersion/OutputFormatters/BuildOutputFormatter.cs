namespace GitFlowVersion
{
    using System.Collections.Generic;
    using System.Linq;

    public static class BuildOutputFormatter
    {
        public static IEnumerable<string> GenerateBuildLogOutput(VersionAndBranch versionAndBranch, IBuildServer buildServer)
        {
            return versionAndBranch.ToKeyValue().Select(variable => buildServer.GenerateSetParameterMessage(variable.Key, variable.Value));
        }
    }
}
