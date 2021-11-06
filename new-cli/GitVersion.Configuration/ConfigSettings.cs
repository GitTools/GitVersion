using GitVersion.Command;

namespace GitVersion.Configuration
{
    [Command("config", "Manages the GitVersion configuration file.")]
    public record ConfigSettings : GitVersionSettings
    {
    }
}