using GitVersion.Command;

namespace GitVersion.Configuration.Init
{
    [Command("init", typeof(ConfigCommand), "Inits the configuration for current repository.")]
    public record ConfigInitCommand : ConfigCommand
    {
    }
}