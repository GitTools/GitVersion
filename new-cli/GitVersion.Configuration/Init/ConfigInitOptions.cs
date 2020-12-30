using GitVersion.Command;

namespace GitVersion.Configuration.Init
{
    [Command("init", typeof(ConfigOptions), "Inits the configuration for current repository.")]
    public record ConfigInitOptions : ConfigOptions
    {
    }
}