using GitVersion.Command;

namespace GitVersion.Configuration.Init
{
    [Command("init", typeof(ConfigSettings), "Inits the configuration for current repository.")]
    public record ConfigInitSettings : ConfigSettings
    {
    }
}