using System.IO.Abstractions;
using GitVersion.Agents;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Output;

namespace GitVersion.Tests;

public class GitVersionCoreTestModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddModule(GitBackendSelector.Resolve() == GitBackend.Managed
            ? new GitVersionManagedGitModule()
            : new GitVersionLibGit2SharpModule());
        services.AddModule(new GitVersionBuildAgentsModule());
        services.AddModule(new GitVersionOutputModule());
        services.AddModule(new GitVersionConfigurationModule());
        services.AddModule(new GitVersionCoreModule());

        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IEnvironment, TestEnvironment>();

        // Override logging with NullLogger for tests (replaces the Serilog-based logging from GitVersionCommonModule)
        services.RemoveAll<ILoggerFactory>();
        services.RemoveAll(typeof(ILogger<>));
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }
}
