using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    public interface ICommandHandler
    {
        IEnumerable<ICommandHandler> GetSubCommands();
    }
    
    public interface ICommandHandler<in T> : ICommandHandler
    {
        Task<int> InvokeAsync(T options);
    }
}