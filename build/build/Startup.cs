using System;
using Cake.Frosting;
using Common.Lifetime;
using Common.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Build
{
    public class Startup : IFrostingStartup
    {
        public void Configure(IServiceCollection services)
        {
            services.UseLifetime<BuildLifetime>();
            services.UseTaskLifetime<BuildTaskLifetime>();

            services.UseWorkingDirectory(Extensions.GetRootDirectory());

            services.UseTool(new Uri("dotnet:?package=Codecov.Tool&version=1.13.0"));
            services.UseTool(new Uri("dotnet:?package=dotnet-format&version=5.0.211103"));
            services.UseTool(new Uri("dotnet:?package=GitVersion.Tool&version=5.6.11"));
        }
    }
}
