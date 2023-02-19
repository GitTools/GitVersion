using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Caching;
using GitVersion.VersionConverters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion;

public class GitVersionCoreModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<IGitVersionCache, GitVersionCache>();
        services.AddSingleton<IGitVersionCacheKeyFactory, GitVersionCacheKeyFactory>();

        services.AddSingleton<IGitVersionCalculateTool, GitVersionCalculateTool>();

        services.AddSingleton<IGitPreparer, GitPreparer>();
        services.AddSingleton<IRepositoryStore, RepositoryStore>();

        services.AddSingleton<IGitVersionContextFactory, GitVersionContextFactory>();
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<GitVersionOptions>>();
            var contextFactory = sp.GetRequiredService<IGitVersionContextFactory>();
            return new Lazy<GitVersionContext>(() => contextFactory.Create(options.Value));
        });

        services.AddModule(new GitVersionCommonModule());
        services.AddModule(new ConfigurationModule());
        services.AddModule(new VersionCalculationModule());
        services.AddModule(new VersionConvertersModule());
    }
}
