namespace GitVersion;

internal static class FeatureSelector
{
    public static string Resolve(string environmentVariableName, string defaultValue, string firstValue, string secondValue)
    {
        var value = SysEnv.GetEnvironmentVariable(environmentVariableName)?.Trim();
        return value switch
        {
            null or "" => defaultValue,
            _ when value.Equals(firstValue, StringComparison.OrdinalIgnoreCase) => firstValue,
            _ when value.Equals(secondValue, StringComparison.OrdinalIgnoreCase) => secondValue,
            _ => throw new WarningException(
                $"Unrecognized {environmentVariableName} value '{value}'. Valid values are '{firstValue}' and '{secondValue}'.")
        };
    }
}
