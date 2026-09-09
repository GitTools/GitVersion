using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.OutputVariables;

internal class VersionVariableSerializer(IFileSystem fileSystem) : IVersionVariableSerializer
{
    public static GitVersionVariables FromJson(string json)
    {
        var variablePairs = JsonSerializer.Deserialize(json, VersionVariablesJsonContext.Custom.DictionaryStringString);
        return FromDictionary(variablePairs);
    }

    public string ToJson(GitVersionVariables gitVersionVariables)
    {
        var variablesType = typeof(VersionVariablesJsonModel);
        var variables = new VersionVariablesJsonModel();

        foreach (var (key, value) in gitVersionVariables.OrderBy(x => x.Key))
        {
            var propertyInfo = variablesType.GetProperty(key);
            propertyInfo?.SetValue(variables, ChangeType(value, propertyInfo.PropertyType));
        }

        return JsonSerializer.Serialize(variables, VersionVariablesJsonContext.Custom.VersionVariablesJsonModel);
    }

    public GitVersionVariables FromFile(string filePath)
    {
        try
        {
            var retryAction = new RetryAction<IOException, GitVersionVariables>();
            return retryAction.Execute(() => FromFileInternal(filePath));
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

    public void ToFile(GitVersionVariables gitVersionVariables, string filePath)
    {
        try
        {
            var retryAction = new RetryAction<IOException>();
            retryAction.Execute(() => ToFileInternal(gitVersionVariables, filePath));
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
        var values = properties?.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.InvariantCultureIgnoreCase);
        var type = typeof(GitVersionVariables);
        var constructors = type.GetConstructors();

        var ctor = constructors.Single();
        var ctorArgs = ctor.GetParameters()
            .Select(p => values?[p.Name!])
            .Cast<object>()
            .ToArray();
        var instance = Activator.CreateInstance(type, ctorArgs).NotNull();
        return (GitVersionVariables)instance with
        {
            SemVerSourceSemVer = NullIfEmpty(values?.GetValueOrDefault(nameof(GitVersionVariables.SemVerSourceSemVer))),
            SemVerSourceSha = NullIfEmpty(values?.GetValueOrDefault(nameof(GitVersionVariables.SemVerSourceSha))),
            SemVerSourceIncrement = NullIfEmpty(values?.GetValueOrDefault(nameof(GitVersionVariables.SemVerSourceIncrement)))
        };
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

    private GitVersionVariables FromFileInternal(string filePath)
    {
        var json = fileSystem.File.ReadAllText(filePath);
        return FromJson(json);
    }

    private void ToFileInternal(GitVersionVariables gitVersionVariables, string filePath)
    {
        var json = ToJson(gitVersionVariables);
        fileSystem.File.WriteAllText(filePath, json);
    }

    private static object? ChangeType(object? value, Type type)
    {
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Nullable<>))
        {
            return Convert.ChangeType(value, type);
        }

        if (value == null || value.ToString()?.Length == 0)
        {
            return null;
        }

        type = Nullable.GetUnderlyingType(type)!;

        return Convert.ChangeType(value, type);
    }
}
