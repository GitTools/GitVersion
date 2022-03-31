namespace GitVersion;

public interface ICommand
{
    Task<int> InvokeAsync(object settings);
}