using System;
using Cake.Frosting;
using Common.Lifetime;
using Microsoft.Extensions.DependencyInjection;

namespace Build
{
    public class Startup : IFrostingStartup
    {
        public void Configure(IServiceCollection services)
        {
            services.UseLifetime<BuildLifetime>();
            services.UseTaskLifetime<BuildTaskLifetime>();

            services.UseTool(new Uri("nuget:?package=nuget.commandline&version=5.8.1"));

            services.UseTool(new Uri("dotnet:?package=Codecov.Tool&version=1.13.0"));
            services.UseTool(new Uri("dotnet:?package=dotnet-format&version=5.0.211103"));
            services.UseTool(new Uri("dotnet:?package=GitReleaseManager.Tool&version=0.11.0"));
            services.UseTool(new Uri("dotnet:?package=GitVersion.Tool&version=5.6.8"));
            services.UseTool(new Uri("dotnet:?package=Wyam.Tool&version=2.2.9"));
        }
    }
}
