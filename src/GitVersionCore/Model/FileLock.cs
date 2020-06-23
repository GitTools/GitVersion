using GitVersion.FileLocking;
using GitVersion.FileLocking.Abstractions;
using System;
using System.IO;

namespace GitVersion.Helpers
{
    public class FileLock : IFileLock
    {
        public FileLockUse FileLockUse { get; }

        public FileLock(FileLockUse fileLockUse)
        {
            if (fileLockUse.Equals(default(FileLockUse)))
            {
                throw new ArgumentNullException(nameof(fileLockUse));
            }

            FileLockUse = fileLockUse;
        }

        public void Dispose() =>
            FileLockUse.Dispose();
    }
}
