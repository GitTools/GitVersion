using System.Threading.Tasks;
using GitVersion.Infrastructure;

namespace GitVersion.Config.Init
{
    public class ConfigInitCommandHandler : CommandHandler<ConfigInitOptions>, IConfigCommandHandler
    {
        private readonly ILogger logger;
        private readonly IService service;

        public ConfigInitCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(ConfigInitOptions options)
        {
            var value = service.Call();
            logger.LogInformation($"Command : 'config init', LogFile : '{options.LogFile}', WorkDir : '{options.WorkDir}' ");
            return Task.FromResult(value);
        }
    }
}