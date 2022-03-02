using Common.Lifetime;
using Microsoft.Extensions.DependencyInjection;

namespace Chores;

public class Startup : IFrostingStartup
{
    public void Configure(IServiceCollection services)
    {
        services.UseLifetime<BuildLifetime>();
        services.UseTaskLifetime<BuildTaskLifetime>();
    }
}
