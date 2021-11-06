using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Normalization
{
    public class NormalizeCommandHandler : CommandHandler<NormalizeSettings>
    {
        private readonly ILogger logger;
        private readonly IService service;

        public NormalizeCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(NormalizeSettings settings)
        {
            var value = service.Call();
            logger.LogInformation(
                $"Command : 'normalize', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
            return Task.FromResult(value);
        }
    }
}