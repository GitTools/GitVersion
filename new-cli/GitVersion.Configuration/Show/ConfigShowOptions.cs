using GitVersion.Command;

namespace GitVersion.Configuration.Show
{
    [Command("show", typeof(ConfigOptions), "Shows the effective configuration.")]
    public record ConfigShowOptions : ConfigOptions
    {
    }
}