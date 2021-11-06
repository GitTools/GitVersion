using GitVersion.Command;

namespace GitVersion.Configuration.Show
{
    [Command("show", typeof(ConfigSettings), "Shows the effective configuration.")]
    public record ConfigShowSettings : ConfigSettings
    {
    }
}