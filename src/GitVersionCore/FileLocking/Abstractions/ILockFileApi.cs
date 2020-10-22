using System.IO;

namespace GitVersion.FileLocking
{
    public interface ILockFileApi
    {
        FileStream WaitUntilAcquired(string filePath, int timeoutInMilliseconds, FileMode fileMode = FileMode.OpenOrCreate, FileAccess fileAccess = FileAccess.ReadWrite, FileShare fileShare = FileShare.None, bool noThrowOnTimeout = false);
    }
}
