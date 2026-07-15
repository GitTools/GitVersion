using GitVersion.Helpers;

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
                // Git marks pack and loose-object files read-only; a bare recursive delete
                // fails on them on Windows. The helper resets attributes before deleting.
                FileSystemHelper.Directory.DeleteDirectory(FullPath);
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
