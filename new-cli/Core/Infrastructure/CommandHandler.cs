using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core
{
    public abstract class CommandHandler<T> : ICommandHandler
    {
        public virtual Task<int> InvokeAsync(T options) => Task.FromResult(0);

        public Task<int> InvokeAsync(object options) => InvokeAsync((T) options);

        public virtual IEnumerable<ICommandHandler> SubCommands() => Enumerable.Empty<ICommandHandler>();
    }
}