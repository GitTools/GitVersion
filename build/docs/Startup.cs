using Common.Lifetime;
using Common.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Docs;

public class Startup : IFrostingStartup
{
    public void Configure(IServiceCollection services)
    {
        services.UseLifetime<BuildLifetime>();
        services.UseTaskLifetime<BuildTaskLifetime>();

        services.UseWorkingDirectory(Extensions.GetRootDirectory());

        services.UseDotnetTool(Tools.GitVersion, Tools.Versions[Tools.GitVersion]);
        services.UseDotnetTool(Tools.Wyam2, Tools.Versions[Tools.Wyam2]);
    }
}
