using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    internal class GitVersionApp : IHostedService
    {
        private readonly IHostApplicationLifetime applicationLifetime;
        private readonly IGitVersionRunner gitVersionRunner;
        private readonly Arguments arguments;

        public GitVersionApp(IHostApplicationLifetime applicationLifetime, IGitVersionRunner gitVersionRunner, IOptions<Arguments> options)
        {
            this.arguments = options.Value;
            this.applicationLifetime = applicationLifetime;
            this.gitVersionRunner = gitVersionRunner;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                gitVersionRunner.Run(arguments);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
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
