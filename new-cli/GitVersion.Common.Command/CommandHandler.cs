using System.Threading.Tasks;

namespace GitVersion.Command
{
    public abstract class CommandHandler<T> : ICommandHandler
    {
        public abstract Task<int> InvokeAsync(T options);
    }
}