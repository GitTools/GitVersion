namespace GitVersion
{
    using System.Collections.Generic;

    public static class BuildOutputFormatter
    {
        public static IEnumerable<string> GenerateBuildLogOutput(IBuildServer buildServer, Dictionary<string, string> variables)
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
