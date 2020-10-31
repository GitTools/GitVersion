using GitVersion.Command;

namespace GitVersion.Configuration.Show
{
    [Command("show", "Shows the effective configuration.")]
    public record ConfigShowOptions : ConfigOptions
    {
    }
}