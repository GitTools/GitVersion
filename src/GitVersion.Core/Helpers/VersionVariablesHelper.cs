using GitVersion.Extensions;
using GitVersion.Helpers;
using YamlDotNet.Serialization;

namespace GitVersion.OutputVariables;

public static class VersionVariablesHelper
{
    public static GitVersionVariables FromJson(string json)
    {
        var serializeOptions = GitVersionVariablesExtensions.GetJsonSerializerOptions();
        var variablePairs = JsonSerializer.Deserialize<Dictionary<string, string>>(json, serializeOptions);
        return FromDictionary(variablePairs);
    }

    public static GitVersionVariables FromFile(string filePath, IFileSystem fileSystem)
    {
        try
        {
            var retryAction = new RetryAction<IOException, GitVersionVariables>();
            return retryAction.Execute(() => FromFileInternal(filePath, fileSystem));
        }
        catch (AggregateException ex)
        {
            var lastException = ex.InnerExceptions.LastOrDefault() ?? ex.InnerException;
            if (lastException != null)
            {
                throw lastException;
            }

            throw;
        }
    }

    private static GitVersionVariables FromDictionary(IEnumerable<KeyValuePair<string, string>>? properties)
    {
        var type = typeof(GitVersionVariables);
        var constructors = type.GetConstructors();

        var ctor = constructors.Single();
        var ctorArgs = ctor.GetParameters()
            .Select(p => properties?.Single(v => string.Equals(v.Key, p.Name, StringComparison.InvariantCultureIgnoreCase)).Value)
            .Cast<object>()
            .ToArray();
        var instance = Activator.CreateInstance(type, ctorArgs).NotNull();
        return (GitVersionVariables)instance;
    }

    private static GitVersionVariables FromFileInternal(string filePath, IFileSystem fileSystem)
    {
        using var stream = fileSystem.OpenRead(filePath);
        using var reader = new StreamReader(stream);
        var dictionary = new Deserializer().Deserialize<Dictionary<string, string>>(reader);
        var versionVariables = FromDictionary(dictionary);
        return versionVariables;
    }
}
