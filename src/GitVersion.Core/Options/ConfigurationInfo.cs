namespace GitVersion;

public record ConfigurationInfo
{
    public string? ConfigurationFile;
    public bool ShowConfiguration;
    public IReadOnlyDictionary<object, object?>? OverrideConfiguration;
}
