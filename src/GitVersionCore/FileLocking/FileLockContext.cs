/*
MIT License

Copyright 2020 Teroneko

Permission is hereby granted, free of charge, to any person obtaining a copy 
of this software and associated documentation files (the "Software"), to deal 
in the Software without restriction, including without limitation the rights to 
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies 
of the Software, and to permit persons to whom the Software is furnished to do 
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all 
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER 
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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
                    this.decreaseLockUseLocker = null;
            }
        }
    }
}
