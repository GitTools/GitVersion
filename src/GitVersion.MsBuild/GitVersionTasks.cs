using GitVersion.Agents;
using GitVersion.MsBuild.Tasks;
using Serilog;
using Serilog.Core;

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

    private static void Configure(IServiceProvider sp)
    {
        var buildAgent = sp.GetRequiredService<ICurrentBuildAgent>();
        var gitVersionOptions = sp.GetRequiredService<IOptions<GitVersionOptions>>().Value;

        if (buildAgent is not LocalBuild)
        {
            gitVersionOptions.Output.Add(OutputType.BuildServer);
        }
        gitVersionOptions.Settings.NoFetch = buildAgent.PreventFetch();
    }

    private static ServiceProvider BuildServiceProvider(GitVersionTaskBase task)
    {
        var bootstrapSwitch = new LoggingLevelSwitch();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(bootstrapSwitch)
            .WriteTo.Console()
            .CreateLogger();

        var services = new ServiceCollection();
        services.AddSingleton(bootstrapSwitch);

        MsBuildHost.RegisterGitVersionModules(services, task);

        var sp = services.BuildServiceProvider();
        Configure(sp);

        return sp;
    }
}
