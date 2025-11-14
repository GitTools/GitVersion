using System.IO.Abstractions;
using GitVersion.OutputVariables;

namespace GitVersion.Core.Tests;

public static class GitVersionVariablesExtensions
{
    extension(GitVersionVariables gitVersionVariables)
    {
        public string ToJson()
        {
            var serializer = new VersionVariableSerializer(new FileSystem());
            return serializer.ToJson(gitVersionVariables);
        }
    }

    extension(string json)
    {
        public GitVersionVariables ToGitVersionVariables()
            => VersionVariableSerializer.FromJson(json);
    }
}
