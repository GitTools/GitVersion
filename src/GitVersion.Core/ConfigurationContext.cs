namespace GitVersion;

public class ConfigurationContext
{
    public string? ConfigurationFile;
    public IReadOnlyDictionary<object, object?>? OverrideConfiguration;
    public bool ShowConfiguration;
}
