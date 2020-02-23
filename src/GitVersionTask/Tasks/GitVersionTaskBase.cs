using System;
using GitVersion.Exceptions;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.MSBuildTask.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion.MSBuildTask
{
    public abstract class GitVersionTaskBase<TTask> : ProxiedTask<TTask>
         where TTask : GitVersionTaskBase<TTask>
    {
        protected GitVersionTaskBase()
        {
            this.Log = new TaskLoggingHelper(this);
        }

        [Required]
        public string SolutionDirectory { get; set; }

        public string ConfigFilePath { get; set; }

        public bool NoFetch { get; set; }

        public bool NoNormalize { get; set; }
        public TaskLoggingHelper Log { get; }

        public override bool OnProxyExecute()
        {
            return ExecuteGitVersionTask();
        }

        protected abstract void ExecuteAction(IGitVersionTaskExecutor executor);
       
        private bool ExecuteGitVersionTask()
        {
            var taskLog = this.Log;
            try
            {
                var sp = BuildServiceProvider();
                var gitVersionTaskExecutor = sp.GetService<IGitVersionTaskExecutor>();

                ExecuteAction(gitVersionTaskExecutor);
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

        private void Configure(IServiceProvider sp)
        {
            var log = sp.GetService<ILog>();
            var buildServerResolver = sp.GetService<IBuildServerResolver>();
            var arguments = sp.GetService<IOptions<Arguments>>().Value;

            log.AddLogAppender(new MsBuildAppender(this.Log));
            var buildServer = buildServerResolver.Resolve();
            arguments.NoFetch = arguments.NoFetch || buildServer != null && buildServer.PreventFetch();
        }

        private IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            var arguments = new Arguments
            {
                TargetPath = SolutionDirectory,
                ConfigFile = ConfigFilePath,
                NoFetch = NoFetch,
                NoNormalize = NoNormalize
            };

            services.AddSingleton(_ => Options.Create(arguments));
            services.AddSingleton<IGitVersionTaskExecutor, GitVersionTaskExecutor>();
            services.AddModule(new GitVersionCoreModule());

            var sp = services.BuildServiceProvider();
            Configure(sp);

            return sp;
        }


    }
}
