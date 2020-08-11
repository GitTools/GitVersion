using System.Collections.Generic;
using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Configuration
{
    public class ConfigCommandHandler : CommandHandler<ConfigOptions>, IRootCommandHandler
    {
        private readonly IEnumerable<IConfigCommandHandler> commandHandlers;

        public ConfigCommandHandler(IEnumerable<IConfigCommandHandler> commandHandlers) => this.commandHandlers = commandHandlers;

        public override IEnumerable<ICommandHandler> SubCommands() => commandHandlers;
    }
}