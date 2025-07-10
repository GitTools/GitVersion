namespace GitVersion.Infrastructure;

public interface ICliApp
{
    Task<int> RunAsync(string[] args, CancellationToken cancellationToken);
}
