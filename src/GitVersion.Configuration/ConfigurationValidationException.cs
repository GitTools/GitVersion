namespace GitVersion.Configuration;

internal sealed class ConfigurationValidationException(string code, string path, string message)
    : ConfigurationException($"Configuration property '{path}': {message}")
{
    public string Code { get; } = code;
    public string PropertyPath { get; } = path;
}
