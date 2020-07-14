using System.Collections.Generic;
using GitVersion.Core.Infrastructure;

namespace GitVersion.Config
{
    public class ConfigCommandHandler : CommandHandler<ConfigOptions>, IRootCommandHandler
    {
        private readonly IEnumerable<IConfigCommandHandler> commandHandlers;

        public ConfigCommandHandler(IEnumerable<IConfigCommandHandler> commandHandlers) => this.commandHandlers = commandHandlers;

        public override IEnumerable<ICommandHandler> SubCommands() => commandHandlers;
    }
}