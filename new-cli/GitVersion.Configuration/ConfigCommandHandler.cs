using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Configuration
{
    public class ConfigCommandHandler : CommandHandler<ConfigCommand>
    {
        private readonly ILogger logger;
        private readonly IService service;

        public ConfigCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }
        
        public override Task<int> InvokeAsync(ConfigCommand command)
        {
            var value = service.Call();
            logger.LogInformation($"Command : 'config', LogFile : '{command.LogFile}', WorkDir : '{command.WorkDir}' ");
            return Task.FromResult(value);
        }
    }
}