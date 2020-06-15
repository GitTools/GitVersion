using System.Collections.Generic;
using System.Threading.Tasks;
using Core;

namespace Output
{
    public class OutputCommandHandler : CommandHandler<OutputOptions>
    {
        private readonly IEnumerable<IOutputCommandHandler> commandHandlers;

        public OutputCommandHandler(IEnumerable<IOutputCommandHandler> commandHandlers)
        {
            this.commandHandlers = commandHandlers;
        }

        public override Task<int> InvokeAsync(OutputOptions options)
        {
            return Task.FromResult(0);
        }

        public override IEnumerable<ICommandHandler> GetSubCommands()
        {
            return commandHandlers;
        }
    }
}