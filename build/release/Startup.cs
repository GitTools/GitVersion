using Common.Lifetime;
using Common.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Release;

public class Startup : IFrostingStartup
{
    public void Configure(IServiceCollection services)
    {
        services.UseLifetime<BuildLifetime>();
        services.UseTaskLifetime<BuildTaskLifetime>();

        services.UseWorkingDirectory(Extensions.GetRootDirectory());

        services.UseTool(new Uri("dotnet:?package=GitVersion.Tool&version=5.6.11"));
        services.UseTool(new Uri("dotnet:?package=GitReleaseManager.Tool&version=0.12.0"));
    }
}
