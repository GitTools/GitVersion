using System;
using System.Threading;
using System.Threading.Tasks;
using GitVersion.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    internal class GitVersionApp : IHostedService
    {
        private readonly IHostApplicationLifetime applicationLifetime;
        private readonly IGitVersionExecutor gitVersionExecutor;
        private readonly ILog log;
        private readonly IOptions<GitVersionOptions> options;

        public GitVersionApp(IHostApplicationLifetime applicationLifetime, IGitVersionExecutor gitVersionExecutor, ILog log, IOptions<GitVersionOptions> options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
            this.gitVersionExecutor = gitVersionExecutor ?? throw new ArgumentNullException(nameof(gitVersionExecutor));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var gitVersionOptions = options.Value;
                log.Verbosity = gitVersionOptions.Verbosity;
                System.Environment.ExitCode = gitVersionExecutor.Execute(gitVersionOptions);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
                System.Environment.ExitCode = 1;
            }

            applicationLifetime.StopApplication();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
