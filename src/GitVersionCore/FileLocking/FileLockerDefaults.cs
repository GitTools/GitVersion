

namespace GitVersion.FileLocking
{
    public static class FileLockerDefaults
    {
        // Represents 90 seconds.
        public const int LockTimeoutInMilliseconds = 1000 * 90;
        public const string LockFileNameWithExtensions = "GitVersion.lock";
    }
}
