using GitVersion.Logging;

namespace GitVersion.MsBuild;

internal class GitVersionMsBuildModule(GitVersionTaskBase task) : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        var gitVersionOptions = new GitVersionOptions { WorkingDirectory = task.SolutionDirectory };

        services.AddSingleton(Options.Create(gitVersionOptions));
        services.AddSingleton<IConsole>(new MsBuildAdapter(task.Log));
        services.AddSingleton<IGitVersionTaskExecutor, GitVersionTaskExecutor>();
    }
}
