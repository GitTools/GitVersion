using GitVersion.Model.Configuration;

namespace GitVersion;

public class ConfigInfo
{
    public string? ConfigFile;
    public GitVersionConfiguration? OverrideConfig;
    public bool ShowConfig;
}
