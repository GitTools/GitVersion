using System.IO.Abstractions;
using GitVersion.OutputVariables;

namespace GitVersion.Core.Tests;

public static class GitVersionVariablesExtensions
{
    public static string ToJson(this GitVersionVariables gitVersionVariables)
    {
        var serializer = new VersionVariableSerializer(new FileSystem());
        return serializer.ToJson(gitVersionVariables);
    }

    public static GitVersionVariables ToGitVersionVariables(this string json)
    {
        var serializer = new VersionVariableSerializer(new FileSystem());
        return serializer.FromJson(json);
    }
}
