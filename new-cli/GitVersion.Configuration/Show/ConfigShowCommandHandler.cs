using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Configuration.Show
{
    public class ConfigShowCommandHandler : CommandHandler<ConfigShowCommand>
    {
        private readonly ILogger logger;
        private readonly IService service;

        public ConfigShowCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(ConfigShowCommand command)
        {
            var value = service.Call();
            logger.LogInformation($"Command : 'config show', LogFile : '{command.LogFile}', WorkDir : '{command.WorkDir}' ");
            return Task.FromResult(value);
        }
    }
}