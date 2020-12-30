using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Output.Project
{
    public class OutputProjectCommandHandler : CommandHandler<OutputProjectCommand>
    {
        private readonly ILogger logger;
        private readonly IService service;

        public OutputProjectCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(OutputProjectCommand command)
        {
            var value = service.Call();
            logger.LogInformation($"Command : 'output project', LogFile : '{command.LogFile}', WorkDir : '{command.OutputDir}', InputFile: '{command.InputFile}', Project: '{command.ProjectFile}' ");
            return Task.FromResult(value);
        }
    }
}