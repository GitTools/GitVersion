using GitVersion.Common;
using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion;

public class GitVersionCoreModule : IGitVersionModule
{
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
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<GitVersionOptions>>();
            var contextFactory = sp.GetRequiredService<IGitVersionContextFactory>();
            return new Lazy<GitVersionContext>(() => contextFactory.Create(options.Value));
        });

        services.AddModule(new GitVersionCommonModule());
        services.AddModule(new VersionCalculationModule());
    }
}
