

namespace GitVersion.FileLocking
{
    public static class FileLockerDefaults
    {
        public const int LockTimeoutInMilliseconds = 1000 * 15;
        public const string LockFileNameWithExtensions = "GitVersion.lock";
    }
}
