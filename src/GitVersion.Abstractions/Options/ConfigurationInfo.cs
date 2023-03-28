namespace GitVersion;

public class ConfigurationInfo
{
    public string? ConfigurationFile;
    public bool ShowConfiguration;
    public IReadOnlyDictionary<object, object?>? OverrideConfiguration;
}
