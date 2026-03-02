using GitVersion.FileSystemGlobbing;

namespace GitVersion;

internal class GitVersionAppModule(string[]? args = null, bool useLegacyParser = false) : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        if (useLegacyParser)
        {
            services.AddSingleton<IArgumentParser, LegacyArgumentParser>();
            services.AddSingleton<IHelpWriter, HelpWriter>();
            services.AddSingleton<IVersionWriter, VersionWriter>();
        }
        else
        {
            services.AddSingleton<IArgumentParser, SystemCommandLineArgumentParser>();
        }

        services.AddSingleton<IGlobbingResolver, GlobbingResolver>();
        services.AddSingleton<IGitVersionExecutor, GitVersionExecutor>();
        services.AddSingleton<GitVersionApp>();

        services.AddSingleton(sp =>
        {
            var arguments = sp.GetRequiredService<IArgumentParser>().ParseArguments(args ?? []);
            var gitVersionOptions = arguments.ToOptions();
            return Options.Create(gitVersionOptions);
        });
    }
}
