namespace GitVersion.Command;

public interface ICommand
{
    Task<int> InvokeAsync(object command);
}