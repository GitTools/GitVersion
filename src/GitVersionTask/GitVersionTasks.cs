using System;
using GitVersion.Exceptions;
using GitVersion.Extensions;
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
                taskLog.LogErrorFromException(exception);
                return false;
            }

            return !taskLog.HasLoggedErrors;
        }

        private static IServiceProvider BuildServiceProvider(GitVersionTaskBase task)
        {
            var services = new ServiceCollection();

            var arguments = new Arguments
            {
                TargetPath = task.SolutionDirectory,
                ConfigFile = task.ConfigFilePath,
                NoFetch = task.NoFetch
            };

            services.AddSingleton(_ => Options.Create(arguments));
            services.AddSingleton<IGitVersionTaskExecutor, GitVersionTaskExecutor>();
            services.AddModule(new GitVersionCoreModule());

            var sp = services.BuildServiceProvider();
            return sp;
        }
    }
}
