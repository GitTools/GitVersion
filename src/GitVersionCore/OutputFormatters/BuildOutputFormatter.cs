using System.Collections.Generic;
using GitVersion.OutputVariables;

namespace GitVersion.OutputFormatters
{
    public static class BuildOutputFormatter
    {
        public static IEnumerable<string> GenerateBuildLogOutput(IBuildServer buildServer, VersionVariables variables)
        {
            var output = new List<string>();

            foreach (var variable in variables)
            {
                output.AddRange(buildServer.GenerateSetParameterMessage(variable.Key, variable.Value));
            }

            return output;
        }
    }
}
