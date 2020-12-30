using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Output.Project
{
    public class OutputProjectCommandHandler : CommandHandler<OutputProjectOptions>
    {
        private readonly ILogger logger;
        private readonly IService service;

        public OutputProjectCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(OutputProjectOptions options)
        {
            var value = service.Call();
            logger.LogInformation($"Command : 'output project', LogFile : '{options.LogFile}', WorkDir : '{options.OutputDir}', InputFile: '{options.InputFile}', Project: '{options.ProjectFile}' ");
            return Task.FromResult(value);
        }
    }
}