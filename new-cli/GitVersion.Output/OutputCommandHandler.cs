using System.Collections.Generic;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Output
{
    public class OutputCommandHandler : CommandHandler<OutputOptions>, IRootCommandHandler
    {
        private readonly IEnumerable<IOutputCommandHandler> commandHandlers;

        public OutputCommandHandler(IEnumerable<IOutputCommandHandler> commandHandlers) => this.commandHandlers = commandHandlers;

        public override IEnumerable<ICommandHandler> SubCommands() => commandHandlers;
    }
}