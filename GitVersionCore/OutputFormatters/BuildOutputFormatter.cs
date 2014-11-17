namespace GitVersion
{
    using System.Collections.Generic;

    public static class BuildOutputFormatter
    {
        public static IEnumerable<string> GenerateBuildLogOutput(SemanticVersion semanticVersion, IBuildServer buildServer)
        {
            var output = new List<string>();

            foreach (var variable in VariableProvider.GetVariablesFor(semanticVersion, new Config()))
            {
                output.AddRange(buildServer.GenerateSetParameterMessage(variable.Key, variable.Value));
            }

            return output;
        }
    }
}
