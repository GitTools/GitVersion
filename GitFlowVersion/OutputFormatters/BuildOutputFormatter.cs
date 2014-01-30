namespace GitFlowVersion
{
    using System.Collections.Generic;
    using System.Linq;

    public static class BuildOutputFormatter
    {
        public static IEnumerable<string> GenerateBuildLogOutput(Dictionary<string, string> variables, IBuildServer buildServer)
        {
            return variables.Select(variable => buildServer.GenerateSetParameterMessage(variable.Key, variable.Value));
        }
    }
}
