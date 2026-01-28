using GitVersion.Agents;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.MsBuild.Tasks;
using GitVersion.Output;

namespace GitVersion.MsBuild;

internal static class GitVersionTasks
{
    public static bool Execute(GitVersionTaskBase task)
    {
        var serviceProvider = BuildServiceProvider(task);
        var executor = serviceProvider.GetRequiredService<IGitVersionTaskExecutor>();
        return task switch
        {
            GetVersion getVersion => ExecuteGitVersionTask(getVersion, executor.GetVersion),
            UpdateAssemblyInfo updateAssemblyInfo => ExecuteGitVersionTask(updateAssemblyInfo, executor.UpdateAssemblyInfo),
            GenerateGitVersionInformation generateGitVersionInformation => ExecuteGitVersionTask(generateGitVersionInformation, executor.GenerateGitVersionInformation),
            WriteVersionInfoToBuildLog writeVersionInfoToBuildLog => ExecuteGitVersionTask(writeVersionInfoToBuildLog, executor.WriteVersionInfoToBuildLog),
            _ => throw new NotSupportedException($"Task type {task.GetType().Name} is not supported")
        };
    }

    private static bool ExecuteGitVersionTask<T>(T task, Action<T> action)
        where T : GitVersionTaskBase
    {
        var taskLog = task.Log;
        try
        {
            action(task);
        }
        catch (WarningException errorException)
        {
            taskLog.LogWarningFromException(errorException);
            return true;
        }
        catch (Exception exception)
        {
            taskLog.LogErrorFromException(exception, true, true, null);
            return false;
        }

        return !taskLog.HasLoggedErrors;
    }

    private static void Configure(IServiceProvider sp, GitVersionTaskBase task)
    {
        var log = sp.GetRequiredService<ILog>();
        var buildAgent = sp.GetRequiredService<ICurrentBuildAgent>();
        var gitVersionOptions = sp.GetRequiredService<IOptions<GitVersionOptions>>().Value;

        log.AddLogAppender(new MsBuildAppender(task.Log));

        if (buildAgent is not LocalBuild)
        {
            gitVersionOptions.Output.Add(OutputType.BuildServer);
        }
        gitVersionOptions.Settings.NoFetch = gitVersionOptions.Settings.NoFetch || buildAgent.PreventFetch();
    }

    private static ServiceProvider BuildServiceProvider(GitVersionTaskBase task)
    {
        var services = new ServiceCollection();

        var gitVersionOptions = new GitVersionOptions
        {
            WorkingDirectory = task.SolutionDirectory
        };

        services.AddSingleton(Options.Create(gitVersionOptions));
        services.AddModule(new GitVersionConfigurationModule());
        services.AddModule(new GitVersionCoreModule());
        services.AddModule(new GitVersionBuildAgentsModule());
        services.AddModule(new GitVersionOutputModule());
        services.AddModule(new GitVersionMsBuildModule());
        services.AddSingleton<IConsole>(new MsBuildAdapter(task.Log));

        var sp = services.BuildServiceProvider();
        Configure(sp, task);

        return sp;
    }
}
