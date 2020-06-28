using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GitVersion.FileLocking
{

#nullable enable

    internal class FileLockContext
    {
        public FileStream? FileStream { get; }
        public Exception? Error { get; }
        public ManualResetEvent? ErrorUnlockDone { get; }

        private readonly FileLocker fileLocker;
        private object? decreaseLockUseLocker;

        private FileLockContext(FileLocker fileLocker, object decreaseLockUseLocker)
        {
            this.fileLocker = fileLocker ?? throw new ArgumentNullException(nameof(fileLocker));
            this.decreaseLockUseLocker = decreaseLockUseLocker;
        }

        public FileLockContext(FileLocker fileLocker, object decreaseLockUseLocker, FileStream fileStream)
            : this(fileLocker, decreaseLockUseLocker)
        {
            fileStream = fileStream ?? throw new ArgumentNullException(nameof(fileStream));
            FileStream = fileStream;
        }

        public FileLockContext(FileLocker fileLocker, object decreaseLockUseLocker, Exception error, ManualResetEvent errorUnlockDone)
            : this(fileLocker, decreaseLockUseLocker)
        {
            Error = error ?? throw new ArgumentNullException(nameof(error));
            ErrorUnlockDone = errorUnlockDone ?? throw new ArgumentNullException(nameof(errorUnlockDone));
        }

        public void DecreaseLockUse(bool decreaseToZero, string lockId)
        {
            if (FileStream == null) {
                throw new InvalidOperationException("You cannot decrease lock use when no file stream has been assgined.");
            }

            var decreaseLockUseLocker = this.decreaseLockUseLocker;

            if (decreaseLockUseLocker == null)
                return;

            // Why surround by lock?
            // There is a race condition, when number of file lock uses
            // is decrased to 0. It may not have invalidated the file
            // stream yet. Now it can happen that the number of file lock
            // uses is increased to 1 due to file lock, but right after another
            // file unlock is about to decrease the number again to 0.
            // There is the possiblity that the actual file lock gets released
            // two times accidentally.
            lock (decreaseLockUseLocker)
            {
                if (!(FileStream.CanRead || FileStream.CanWrite))
                {
                    Trace.WriteLine($"{FileLocker.CurrentThreadWithLockIdPrefix(lockId)} Lock use has been invalidated before. Skip decreasing lock use.", FileLocker.TraceCategory);
                    return;
                }

                var locksInUse = fileLocker.DecreaseLockUse(decreaseToZero, lockId);

                if (0 == locksInUse)
                {
                    this.decreaseLockUseLocker = null;
                }
            }
        }
    }
}
