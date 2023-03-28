using System.Text.Encodings.Web;
using GitVersion.OutputVariables;

namespace GitVersion.Extensions;

public static class GitVersionVariablesExtensions
{
    public static string ToJsonString(this GitVersionVariables gitVersionVariables)
    {
        var variablesType = typeof(VersionVariablesJsonModel);
        var variables = new VersionVariablesJsonModel();

        foreach (var (key, value) in gitVersionVariables.OrderBy(x => x.Key))
        {
            var propertyInfo = variablesType.GetProperty(key);
            propertyInfo?.SetValue(variables, ChangeType(value, propertyInfo.PropertyType));
        }

        var serializeOptions = GetJsonSerializerOptions();

        return JsonSerializer.Serialize(variables, serializeOptions);
    }

    public static JsonSerializerOptions GetJsonSerializerOptions()
    {
        var serializeOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters =
            {
                new VersionVariablesJsonStringConverter()
            }
        };
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
