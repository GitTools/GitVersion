using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Configuration.Init
{
    public class ConfigInitCommandHandler : CommandHandler<ConfigInitCommand>
    {
        private readonly ILogger logger;
        private readonly IService service;

        public ConfigInitCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(ConfigInitCommand command)
        {
            var value = service.Call();
            logger.LogInformation($"Command : 'config init', LogFile : '{command.LogFile}', WorkDir : '{command.WorkDir}' ");
            return Task.FromResult(value);
        }
    }
}