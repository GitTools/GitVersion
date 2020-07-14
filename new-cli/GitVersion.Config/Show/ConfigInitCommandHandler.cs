using System;
using System.Threading.Tasks;
using GitVersion.Core;
using GitVersion.Core.Infrastructure;

namespace GitVersion.Config.Show
{
    public class ConfigShowCommandHandler : CommandHandler<ConfigShowOptions>, IConfigCommandHandler
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