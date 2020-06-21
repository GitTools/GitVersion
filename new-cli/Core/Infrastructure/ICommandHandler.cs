using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    public interface ICommandHandler
    {
        Task<int> InvokeAsync(object options);

        IEnumerable<ICommandHandler> SubCommands();
    }
}