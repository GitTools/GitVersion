using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core
{
    public abstract class CommandHandler<T> : ICommandHandler<T>
    {
        public abstract Task<int> InvokeAsync(T options);

        public virtual IEnumerable<ICommandHandler> GetSubCommands()
        {
            return Enumerable.Empty<ICommandHandler>();
        }
    }
}