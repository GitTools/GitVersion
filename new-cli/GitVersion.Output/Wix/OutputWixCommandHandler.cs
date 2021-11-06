using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Output.Wix
{
    public class OutputWixCommandHandler : CommandHandler<OutputWixSettings>
    {
        private readonly ILogger logger;
        private readonly IService service;

        public OutputWixCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(OutputWixSettings settings)
        {
            var value = service.Call();
            logger.LogInformation(
                $"Command : 'output wix', LogFile : '{settings.LogFile}', WorkDir : '{settings.OutputDir}', InputFile: '{settings.InputFile}', WixFile: '{settings.WixFile}' ");
            return Task.FromResult(value);
        }
    }
}