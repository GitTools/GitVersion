using System;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model;
using GitVersion.MSBuildTask.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion.MSBuildTask
{
    public static class GitVersionTasks
    {
        public static bool GetVersion(GetVersion task) => ExecuteGitVersionTask(task, executor => executor.GetVersion(task));

        public static bool UpdateAssemblyInfo(UpdateAssemblyInfo task) => ExecuteGitVersionTask(task, executor => executor.UpdateAssemblyInfo(task));

        public static bool GenerateGitVersionInformation(GenerateGitVersionInformation task) => ExecuteGitVersionTask(task, executor => executor.GenerateGitVersionInformation(task));

        public static bool WriteVersionInfoToBuildLog(WriteVersionInfoToBuildLog task) => ExecuteGitVersionTask(task, executor => executor.WriteVersionInfoToBuildLog(task));

        private static bool ExecuteGitVersionTask<T>(T task, Action<IGitVersionTaskExecutor> action)
            where T : GitVersionTaskBase
        {
            var taskLog = task.Log;
            try
            {
                var sp = BuildServiceProvider(task);
                var gitVersionTaskExecutor = sp.GetService<IGitVersionTaskExecutor>();

                action(gitVersionTaskExecutor);
            }
            catch (WarningException errorException)
            {
                taskLog.LogWarningFromException(errorException);
                return true;
            }
            catch (Exception exception)
            {
                taskLog.LogErrorFromException(exception, showStackTrace: true, showDetail: true, null);
                return false;
            }

            return !taskLog.HasLoggedErrors;
        }

        private static void Configure(IServiceProvider sp, GitVersionTaskBase task)
        {
            var log = sp.GetService<ILog>();
            var buildAgent = sp.GetService<ICurrentBuildAgent>();
            var gitVersionOptions = sp.GetService<IOptions<GitVersionOptions>>().Value;

            log.AddLogAppender(new MsBuildAppender(task.Log));

            if (buildAgent != null)
            {
                gitVersionOptions.Output.Add(OutputType.BuildServer);
            }
            gitVersionOptions.Settings.NoFetch = gitVersionOptions.Settings.NoFetch || buildAgent != null && buildAgent.PreventFetch();
        }

        private static IServiceProvider BuildServiceProvider(GitVersionTaskBase task)
        {
            var services = new ServiceCollection();

            var gitVersionOptions = new GitVersionOptions
            {
                WorkingDirectory = task.SolutionDirectory,
                ConfigInfo = { ConfigFile = task.ConfigFilePath },
                Settings =
                {
                    NoFetch = task.NoFetch,
                    NoNormalize = task.NoNormalize
                }
            };

            gitVersionOptions.Output.Add(OutputType.BuildServer);

            services.AddSingleton(Options.Create(gitVersionOptions));
            services.AddModule(new GitVersionCoreModule());
            services.AddModule(new GitVersionTaskModule());

            services.AddSingleton<IConsole>(new MsBuildAdapter(task.Log));

            var sp = services.BuildServiceProvider();
            Configure(sp, task);

            return sp;
        }
    }
}
