using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Output.Wix
{
    public class OutputWixCommandHandler : CommandHandler<OutputWixCommand>
    {
        private readonly ILogger logger;
        private readonly IService service;

        public OutputWixCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(OutputWixCommand command)
        {
            var value = service.Call();
            logger.LogInformation($"Command : 'output wix', LogFile : '{command.LogFile}', WorkDir : '{command.OutputDir}', InputFile: '{command.InputFile}', WixFile: '{command.WixFile}' ");
            return Task.FromResult(value);
        }
    }
}