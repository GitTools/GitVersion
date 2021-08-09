using System;
using Cake.Frosting;
using Common.Lifetime;
using Microsoft.Extensions.DependencyInjection;

namespace Docs
{
    public class Startup : IFrostingStartup
    {
        public void Configure(IServiceCollection services)
        {
            services.UseLifetime<BuildLifetime>();
            services.UseTaskLifetime<BuildTaskLifetime>();

            services.UseTool(new Uri("dotnet:?package=Wyam.Tool&version=2.2.9"));
        }
    }
}
