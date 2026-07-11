namespace GitVersion.Git.Managed.Tests;

/// <summary>
/// Reserves a unique temporary directory path (without creating it, so it can be used as
/// a target for <c>git clone</c> or <c>git worktree add</c>) and deletes it on dispose.
/// </summary>
internal sealed class TempDirectory : IDisposable
{
    public string FullPath { get; } = Path.Combine(Path.GetTempPath(), "managed-git-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(FullPath))
            {
                Directory.Delete(FullPath, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best effort cleanup of the temporary directory.
        }
        catch (UnauthorizedAccessException)
        {
            // Best effort cleanup of the temporary directory.
        }
    }
}
