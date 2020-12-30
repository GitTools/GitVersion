using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Normalization
{
    public class NormalizeCommandHandler : CommandHandler<NormalizeCommand>
    {
        private readonly ILogger logger;
        private readonly IService service;

        public NormalizeCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(NormalizeCommand command)
        {
            var value = service.Call();
            logger.LogInformation($"Command : 'normalize', LogFile : '{command.LogFile}', WorkDir : '{command.WorkDir}' ");
            return Task.FromResult(value);
        }
    }
}