using System;
using System.Threading.Tasks;
using GitVersion.Core;
using GitVersion.Core.Infrastructure;

namespace GitVersion.Calculate
{
    public class CalculateCommandHandler : CommandHandler<CalculateOptions>, IRootCommandHandler
    {
        private readonly ILogger logger;
        private readonly IService service;

        public CalculateCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(CalculateOptions options)
        {
            var value = service.Call();
            logger.LogInformation($"Command : 'calculate', LogFile : '{options.LogFile}', WorkDir : '{options.WorkDir}' ");
            return Task.FromResult(value);
        }
    }
}