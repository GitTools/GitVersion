using GitVersion.Logging;
using Microsoft.Extensions.Hosting;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace GitVersion.Cli
{
    internal class HostedService : IHostedService
    {
        private readonly IHostApplicationLifetime applicationLifetime;
        private readonly RootCommand command;
        // private readonly IGitVersionExecutor gitVersionExecutor;
        private readonly ILog log;
        private readonly string[] arguments;

        public HostedService(IHostApplicationLifetime applicationLifetime, RootCommand command, ILog log, string[] arguments)
        {

            this.applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
            this.command = command;
            // this.gitVersionExecutor = gitVersionExecutor ?? throw new ArgumentNullException(nameof(gitVersionExecutor));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.arguments = arguments;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                System.Environment.ExitCode = await command.InvokeAsync(arguments);
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
