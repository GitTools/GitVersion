namespace GitVersion;

public interface ICommand<in T>
{
    public Task<int> InvokeAsync(T settings);
}

public interface ICommandImpl
{
    string CommandName { get; }
    string ParentCommandName { get; }
}
