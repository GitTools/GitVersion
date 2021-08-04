using System;
using Cake.Frosting;
using Common.Lifetime;
using Microsoft.Extensions.DependencyInjection;

namespace Publish
{
    public class Startup : IFrostingStartup
    {
        public void Configure(IServiceCollection services)
        {
            services.UseLifetime<BuildLifetime>();
            services.UseTaskLifetime<BuildTaskLifetime>();

            services.UseTool(new Uri("dotnet:?package=GitVersion.Tool&version=5.6.8"));
        }
    }
}
