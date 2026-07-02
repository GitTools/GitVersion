using System.IO.Abstractions;
using GitVersion.Agents;
using GitVersion.Extensions;

namespace GitVersion;

/// <summary>Registers common infrastructure services such as logging, file system, environment, and build-agent resolution.</summary>
public class GitVersionCommonModule : IGitVersionModule
{
    /// <summary>Registers the common infrastructure services into the DI container.</summary>
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
