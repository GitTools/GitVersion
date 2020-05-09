using System;
using System.CommandLine;
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
        private readonly GitVersionRootCommand command;
        // private readonly IGitVersionExecutor gitVersionExecutor;
        private readonly ILog log;
        private readonly IOptions<GitVersionOptions> options;

        public GitVersionApp(IHostApplicationLifetime applicationLifetime, GitVersionRootCommand command, ILog log, IOptions<GitVersionOptions> options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
            this.command = command;
            // this.gitVersionExecutor = gitVersionExecutor ?? throw new ArgumentNullException(nameof(gitVersionExecutor));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                System.Environment.ExitCode = await command.InvokeAsync(options.Value.Args);
                //  log.Verbosity = gitVersionOptions.Verbosity; // not sure how to get verbosity now.
            }
            catch (Exception exception)
            {                
                Console.Error.WriteLine(exception.Message);
                System.Environment.ExitCode = 1;
            }

            applicationLifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
