using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Configuration.Show
{
    public class ConfigShowCommandHandler : CommandHandler<ConfigShowOptions>
    {
        private readonly ILogger logger;
        private readonly IService service;

        public ConfigShowCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(ConfigShowOptions options)
        {
            var value = service.Call();
            logger.LogInformation($"Command : 'config show', LogFile : '{options.LogFile}', WorkDir : '{options.WorkDir}' ");
            return Task.FromResult(value);
        }
    }
}