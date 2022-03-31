namespace GitVersion;

public class GitVersionApp
{
    public Task<int> RunAsync(string[] args)
    {
        Console.WriteLine("Testing application for the GitVersion.Core without the command processing");
        return ValueTask.FromResult(0).AsTask();
    }
}
