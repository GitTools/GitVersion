using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Configuration
{
    public class ConfigCommandHandler : CommandHandler<ConfigOptions>
    {
        private readonly ILogger logger;
        private readonly IService service;

        public ConfigCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }
        
        public override Task<int> InvokeAsync(ConfigOptions options)
        {
            var value = service.Call();
            logger.LogInformation($"Command : 'config', LogFile : '{options.LogFile}', WorkDir : '{options.WorkDir}' ");
            return Task.FromResult(value);
        }
    }
}