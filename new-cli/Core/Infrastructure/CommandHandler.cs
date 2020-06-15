using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core
{
    public abstract class CommandHandler<T> : ICommandHandler<T>
    {
        public virtual Task<int> InvokeAsync(T options) => Task.FromResult(0);

        public virtual IEnumerable<ICommandHandler> GetSubCommands() => Enumerable.Empty<ICommandHandler>();
    }
}