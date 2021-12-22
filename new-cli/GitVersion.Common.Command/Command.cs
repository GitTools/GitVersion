namespace GitVersion.Command;

public abstract class Command<T> : ICommand
    where T : GitVersionSettings
{
    public abstract Task<int> InvokeAsync(T settings);
    Task<int> ICommand.InvokeAsync(object settings) => InvokeAsync((T) settings);
}