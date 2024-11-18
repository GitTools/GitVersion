using GitVersion.Agents;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Output;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Core.Tests.Helpers;

public class GitVersionCoreTestModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddModule(new GitVersionLibGit2SharpModule());
        services.AddModule(new GitVersionBuildAgentsModule());
        services.AddModule(new GitVersionOutputModule());
        services.AddModule(new GitVersionConfigurationModule());
        services.AddModule(new GitVersionCoreModule());

        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IEnvironment, TestEnvironment>();
        services.AddSingleton<ILog, NullLog>();
    }
}
