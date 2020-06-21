using System.Collections.Generic;
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

        public override IEnumerable<ICommandHandler> SubCommands() => commandHandlers;
    }
}