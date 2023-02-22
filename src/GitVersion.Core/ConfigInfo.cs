namespace GitVersion;

public class ConfigInfo
{
    public string? ConfigFile;
    public IReadOnlyDictionary<object, object?>? OverrideConfiguration;
    public bool ShowConfig;
}
