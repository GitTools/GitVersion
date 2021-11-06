using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Configuration.Init
{
    public class ConfigInitCommandHandler : CommandHandler<ConfigInitSettings>
    {
        private readonly ILogger logger;
        private readonly IService service;

        public ConfigInitCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(ConfigInitSettings settings)
        {
            var value = service.Call();
            logger.LogInformation(
                $"Command : 'config init', LogFile : '{settings.LogFile}', WorkDir : '{settings.WorkDir}' ");
            return Task.FromResult(value);
        }
    }
}