using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Normalization
{
    public class NormalizeCommandHandler : CommandHandler<NormalizeOptions>, IRootCommandHandler
    {
        private readonly ILogger logger;
        private readonly IService service;

        public NormalizeCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(NormalizeOptions options)
        {
            var value = service.Call();
            logger.LogInformation($"Command : 'normalize', LogFile : '{options.LogFile}', WorkDir : '{options.WorkDir}' ");
            return Task.FromResult(value);
        }
    }
}