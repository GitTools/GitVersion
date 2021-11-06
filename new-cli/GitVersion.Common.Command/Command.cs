using System.Threading.Tasks;

namespace GitVersion.Command;

public abstract class Command<T> : ICommand
    where T : GitVersionSettings
{
    public abstract Task<int> InvokeAsync(T command);
    Task<int> ICommand.InvokeAsync(object command) => InvokeAsync((T) command);
}