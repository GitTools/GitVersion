using GitVersion.FileSystemGlobbing;

namespace GitVersion;

internal class GitVersionAppModule(params string[] args) : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<IArgumentParser, ArgumentParser>();
        services.AddSingleton<IGlobbingResolver, GlobbingResolver>();

        services.AddSingleton<IHelpWriter, HelpWriter>();
        services.AddSingleton<IVersionWriter, VersionWriter>();
        services.AddSingleton<IGitVersionExecutor, GitVersionExecutor>();
        services.AddSingleton<GitVersionApp>();

        services.AddSingleton(sp =>
        {
            var arguments = sp.GetRequiredService<IArgumentParser>().ParseArguments(args);
            var gitVersionOptions = arguments.ToOptions();
            return Options.Create(gitVersionOptions);
        });
    }
}
