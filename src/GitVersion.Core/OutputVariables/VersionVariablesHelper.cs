using System.Text.Encodings.Web;
using GitVersion.Extensions;
using GitVersion.Helpers;
using YamlDotNet.Serialization;

namespace GitVersion.OutputVariables;

public static class VersionVariablesHelper
{
    public static GitVersionVariables FromJson(string json)
    {
        var serializeOptions = JsonSerializerOptions();
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

    public static string ToJsonString(this GitVersionVariables gitVersionVariables)
    {
        var variablesType = typeof(VersionVariablesJsonModel);
        var variables = new VersionVariablesJsonModel();

        foreach (var (key, value) in gitVersionVariables.OrderBy(x => x.Key))
        {
            var propertyInfo = variablesType.GetProperty(key);
            propertyInfo?.SetValue(variables, ChangeType(value, propertyInfo.PropertyType));
        }

        var serializeOptions = JsonSerializerOptions();

        return JsonSerializer.Serialize(variables, serializeOptions);
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

    private static JsonSerializerOptions JsonSerializerOptions()
    {
        var serializeOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, Converters = { new VersionVariablesJsonStringConverter() } };
        return serializeOptions;
    }

    private static object? ChangeType(object? value, Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            if (value == null || value.ToString()?.Length == 0)
            {
                return null;
            }

            type = Nullable.GetUnderlyingType(type)!;
        }

        return Convert.ChangeType(value, type);
    }
}
