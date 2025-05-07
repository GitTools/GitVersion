namespace GitVersion;

internal interface IGitVersionAppRunner
{
    Task<int> RunAsync(string[] args, CancellationToken cancellationToken);
}
