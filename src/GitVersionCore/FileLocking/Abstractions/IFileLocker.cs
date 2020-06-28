namespace GitVersion.FileLocking
{
    public interface IFileLocker
    {
        FileLockUse WaitUntilAcquired();
    }
}
