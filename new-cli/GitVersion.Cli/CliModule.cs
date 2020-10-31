using GitVersion.Cli.Infrastructure;
using GitVersion.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ILogger = GitVersion.Infrastructure.ILogger;

namespace GitVersion.Cli
{
    public class CliModule : IGitVersionModule
    {
        public void RegisterTypes(IContainerRegistrar services)
        {
            services.AddConsoleLogging();
            services.AddSingleton<ILogger>(provider => new Logger(provider.GetService<ILogger<Logger>>()!));

            services.AddSingleton<GitVersionApp>();
        }
    }
}