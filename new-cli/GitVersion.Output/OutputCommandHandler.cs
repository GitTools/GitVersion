using System.Threading.Tasks;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Output
{
    public class OutputCommandHandler : CommandHandler<OutputCommand>
    {
        private readonly ILogger logger;
        private readonly IService service;

        public OutputCommandHandler(ILogger logger, IService service)
        {
            this.logger = logger;
            this.service = service;
        }

        public override Task<int> InvokeAsync(OutputCommand command)
        {
            var value = service.Call();
            logger.LogInformation(
                $"Command : 'output', LogFile : '{command.LogFile}', WorkDir : '{command.WorkDir}' ");
            return Task.FromResult(value);
        }
    }
}