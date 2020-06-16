using System.Collections.Generic;

namespace Core
{
    public interface ICommandHandler
    {
        IEnumerable<ICommandHandler> GetSubCommands();
    }
}