using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Configuration.Show
{
    public class ConfigShowCommandHandler : CommandHandler<ConfigShowSettings>
    {
        private readonly ILogger logger;
        private readonly IService service;

        public ConfigShowCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(ConfigShowSettings settings)
        {
            var value = service.Call();
            logger.LogInformation(
                $"Command : 'config show', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
            return Task.FromResult(value);
        }
    }
}