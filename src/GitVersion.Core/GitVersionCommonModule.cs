using System.IO.Abstractions;
using GitVersion.Agents;
using GitVersion.Extensions;

namespace GitVersion;

public class GitVersionCommonModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddGitVersionLogging();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IEnvironment, Environment>();
        services.AddSingleton<IConsole, ConsoleAdapter>();

        services.AddSingleton<IBuildAgent, LocalBuild>();
        services.AddSingleton<IBuildAgentResolver, BuildAgentResolver>();
        services.AddSingleton(sp => sp.GetRequiredService<IBuildAgentResolver>().Resolve());
    }
}
