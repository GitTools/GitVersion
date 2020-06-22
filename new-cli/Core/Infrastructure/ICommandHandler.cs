using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    public interface ICommandHandler
    {
        Task<int> InvokeAsync(GitVersionOptions options);

        IEnumerable<ICommandHandler> SubCommands();
    }
}