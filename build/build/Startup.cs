using Common.Lifetime;
using Common.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Build;

public class Startup : IFrostingStartup
{
    public void Configure(IServiceCollection services)
    {
        services.UseLifetime<BuildLifetime>();
        services.UseTaskLifetime<BuildTaskLifetime>();

        services.UseWorkingDirectory(Extensions.GetRootDirectory());

        services.UseNugetTool(Tools.NugetCmd, Tools.Versions[Tools.NugetCmd]);
        services.UseDotnetTool(Tools.Codecov, Tools.Versions[Tools.Codecov]);
        services.UseDotnetTool(Tools.GitVersion, Tools.Versions[Tools.GitVersion]);
    }
}
