namespace GitVersion
{
    using System.Collections.Generic;

    public static class BuildOutputFormatter
    {
        public static IEnumerable<string> GenerateBuildLogOutput(VersionAndBranch versionAndBranch, IBuildServer buildServer)
        {
            var output = new List<string>();

            foreach (var variable in versionAndBranch.ToKeyValue())
            {
                output.AddRange(buildServer.GenerateSetParameterMessage(variable.Key, variable.Value));
            }

            return output;
        }
    }
}
