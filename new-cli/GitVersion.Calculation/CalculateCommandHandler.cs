using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Calculation
{
    public class CalculateCommandHandler : CommandHandler<CalculateOptions>
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
            logger.LogInformation(
                $"Command : 'calculate', LogFile : '{options.LogFile}', WorkDir : '{options.WorkDir}' ");
            return Task.FromResult(value);
        }
    }
}