using GitVersion.Command;

namespace GitVersion.Configuration.Show
{
    [Command("show", typeof(ConfigCommand), "Shows the effective configuration.")]
    public record ConfigShowCommand : ConfigCommand
    {
    }
}