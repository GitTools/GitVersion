using GitVersion.BuildAgents;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Cache;
using GitVersion.VersionConverters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion;

public class GitVersionCoreModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<ILog, Log>();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IEnvironment, Environment>();
        services.AddSingleton<IConsole, ConsoleAdapter>();

        services.AddSingleton<IGitVersionCache, GitVersionCache>();
        services.AddSingleton<IGitVersionCacheKeyFactory, GitVersionCacheKeyFactory>();

        services.AddSingleton<IGitVersionCalculateTool, GitVersionCalculateTool>();
        services.AddSingleton<IGitVersionOutputTool, GitVersionOutputTool>();

        services.AddSingleton<IGitPreparer, GitPreparer>();
        services.AddSingleton<IRepositoryStore, RepositoryStore>();

        services.AddSingleton<IGitVersionContextFactory, GitVersionContextFactory>();
        services.AddSingleton(sp =>
        {
            var options = sp.GetService<IOptions<GitVersionOptions>>();
            var contextFactory = sp.GetService<IGitVersionContextFactory>();
            return new Lazy<GitVersionContext?>(() => contextFactory?.Create(options?.Value));
        });

        services.AddModule(new BuildServerModule());
        services.AddModule(new ConfigurationModule());
        services.AddModule(new VersionCalculationModule());
        services.AddModule(new VersionConvertersModule());
    }
}
