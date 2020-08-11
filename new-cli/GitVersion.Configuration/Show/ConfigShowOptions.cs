using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Configuration.Show
{
    [Command("show", "Shows the effective configuration.")]
    public class ConfigShowOptions : ConfigOptions
    {
    }
}