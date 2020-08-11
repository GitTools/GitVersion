using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Output.Wix
{
    public class OutputWixCommandHandler : CommandHandler<OutputWixOptions>, IOutputCommandHandler
    {
        private readonly ILogger logger;
        private readonly IService service;

        public OutputWixCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(OutputWixOptions options)
        {
            var value = service.Call();
            logger.LogInformation($"Command : 'output wix', LogFile : '{options.LogFile}', WorkDir : '{options.OutputDir}', InputFile: '{options.InputFile}', WixFile: '{options.WixFile}' ");
            return Task.FromResult(value);
        }
    }
}