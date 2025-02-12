namespace GitVersion;

public interface ICommand<in T>
{
    Task<int> InvokeAsync(T settings, CancellationToken cancellationToken = default);
}

public interface ICommandImpl
{
    string CommandName { get; }
    string ParentCommandName { get; }
}
