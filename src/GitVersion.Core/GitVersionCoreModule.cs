using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Caching;

namespace GitVersion;

/// <summary>Registers the core GitVersion services including version calculation, repository access, and caching.</summary>
public class GitVersionCoreModule : IGitVersionModule
{
    /// <summary>Registers all core services into the DI container.</summary>
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<IGitVersionCacheProvider, GitVersionCacheProvider>();

        services.AddSingleton<IGitVersionCalculateTool, GitVersionCalculateTool>();

        services.AddSingleton<IGitPreparer, GitPreparer>();
        services.AddSingleton<IRepositoryStore, RepositoryStore>();
        services.AddSingleton<ITaggedSemanticVersionRepository, TaggedSemanticVersionRepository>();
        services.AddSingleton<ITaggedSemanticVersionService, TaggedSemanticVersionService>();
        services.AddSingleton<IBranchRepository, BranchRepository>();

        services.AddSingleton<IGitVersionContextFactory, GitVersionContextFactory>();
        services.AddSingleton(sp => new Lazy<IGitVersionConfiguration>(() =>
        {
            var configurationProvider = sp.GetRequiredService<IConfigurationProvider>();
            var options = sp.GetRequiredService<IOptions<GitVersionOptions>>();
            return configurationProvider.Provide(options.Value.ConfigurationInfo.OverrideConfiguration);
        }));
        services.AddSingleton(sp =>
        {
            var contextFactory = sp.GetRequiredService<IGitVersionContextFactory>();
            return new Lazy<GitVersionContext>(() => contextFactory.Create());
        });

        services.AddModule(new GitVersionCommonModule());
        services.AddModule(new VersionCalculationModule());
    }
}
