using GitVersion.BuildAgents;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.VersionConverters.AssemblyInfo;
using GitVersion.VersionConverters.GitVersionInfo;
using GitVersion.VersionConverters.OutputGenerator;
using GitVersion.VersionConverters.WixUpdater;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.MsBuild;

public class GitVersionTaskModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<ILog, Log>();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IEnvironment, Environment>();
        services.AddSingleton<IConsole, ConsoleAdapter>();

        services.AddSingleton<IGitVersionTaskExecutor, GitVersionTaskExecutor>();

        services.AddSingleton<IGitVersionOutputTool, GitVersionOutputTool>();
        services.AddSingleton<IOutputGenerator, OutputGenerator>();
        services.AddSingleton<IGitVersionInfoGenerator, GitVersionInfoGenerator>();
        services.AddSingleton<IWixVersionFileUpdater, WixVersionFileUpdater>();
        services.AddSingleton<IAssemblyInfoFileUpdater, AssemblyInfoFileUpdater>();
        services.AddSingleton<IProjectFileUpdater, ProjectFileUpdater>();

        services.AddSingleton<IBuildAgentResolver, BuildAgentResolver>();
        services.AddModule(new BuildServerModule());
        services.AddSingleton(sp => sp.GetService<IBuildAgentResolver>()?.Resolve());
    }
}
