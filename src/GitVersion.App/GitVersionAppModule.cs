using Microsoft.Extensions.DependencyInjection;

namespace GitVersion;

public class GitVersionAppModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<IArgumentParser, ArgumentParser>();
        services.AddSingleton<IGlobbingResolver, GlobbingResolver>();

        services.AddSingleton<IHelpWriter, HelpWriter>();
        services.AddSingleton<IVersionWriter, VersionWriter>();
        services.AddSingleton<IGitVersionExecutor, GitVersionExecutor>();
    }
}
