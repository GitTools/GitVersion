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
using System.IO;
using System.Threading;

namespace GitVersion.FileLocking
{

#nullable enable

    /// <summary>
    /// This helper class can lock files.
    /// </summary>
    public class LockFileApi : ILockFileApi
    {
        public const FileMode DefaultFileMode = FileMode.OpenOrCreate;
        public const FileAccess DefaultFileAccess = FileAccess.ReadWrite;
        public const FileShare DefaultFileShare = FileShare.None;
        public const int DefaultTimeoutInMilliseconds = Timeout.Infinite;

        private readonly IFileSystem fileSystem;

        public LockFileApi(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        /// <summary>
        /// Try to acquire lock on file but only as long the file stream is opened.
        /// </summary>
        /// <param name="filePath">The path to file that get locked.</param>
        /// <param name="fileStream">The locked file as file stream.</param>
        /// <param name="fileMode">The file mode when opening file.</param>
        /// <param name="fileAccess">The file access when opening file.</param>
        /// <param name="fileShare">The file share when opening file</param>
        /// <returns>If true the lock acquirement was successful.</returns>
        public bool TryAcquire(string filePath, out FileStream? fileStream, FileMode fileMode = DefaultFileMode,
            FileAccess fileAccess = DefaultFileAccess, FileShare fileShare = DefaultFileShare)
        {
            filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

            try
            {
                fileStream = fileSystem.Open(filePath, fileMode, fileAccess, fileShare);
                // Add UNIX support (reference https://github.com/dotnet/coreclr/pull/8233).
                fileStream.Lock(0, 0);
                return true;
            }
            // The IOException does specify that the file could not been accessed because 
            // it was partially locked. All other exception have to be handled by consumer.
            // 
            // See references:
            // https://docs.microsoft.com/en-US/dotnet/api/system.io.file.open?view=netcore-3.1 (exceptions)
            // https://docs.microsoft.com/en-US/dotnet/api/system.io.filestream.lock?view=netcore-3.1#exceptions
            catch (Exception error) when (error.GetType() == typeof(IOException))
            {
                fileStream = null;
                return false;
            }
        }

        /// <summary>
        /// Try to acquire lock on file but only as long the file stream is opened.
        /// </summary>
        /// <param name="filePath">The path to file that get locked.</param>
        /// <param name="fileMode">The file mode when opening file.</param>
        /// <param name="fileAccess">The file access when opening file.</param>
        /// <param name="fileShare">The file share when opening file</param>
        /// <returns>If not null the lock acquirement was successful.</returns>
        public FileStream? TryAcquire(string filePath, FileMode fileMode = DefaultFileMode,
            FileAccess fileAccess = DefaultFileAccess, FileShare fileShare = DefaultFileShare)
        {
            TryAcquire(filePath, out var fileStream, fileMode: fileMode,
                fileAccess: fileAccess, fileShare: fileShare);

            return fileStream;
        }

        private bool waitUntilAcquired(string filePath, out FileStream? fileStream, FileMode fileMode,
            FileAccess fileAccess, FileShare fileShare, int timeoutInMilliseconds, bool throwOnTimeout)
        {
            FileStream? spinningFileStream = null;

            var spinHasBeenFinished = SpinWait.SpinUntil(() =>
            {
                return TryAcquire(filePath, out spinningFileStream, fileMode: fileMode, fileAccess: fileAccess, fileShare: fileShare);
            }, timeoutInMilliseconds);

            if (spinHasBeenFinished)
            {
                fileStream = spinningFileStream ?? throw new ArgumentNullException(nameof(spinningFileStream));
                return true;
            }
            else
            {
                if (throwOnTimeout)
                {
                    throw new TimeoutException($"Acquiring file lock failed due to timeout.");
                }

                fileStream = null;
                return false;
            }
        }

        private FileStream? waitUntilAcquired(string filePath, FileMode fileMode,
            FileAccess fileAccess, FileShare fileShare, int timeoutInMilliseconds, bool noThrowOnTimeout)
        {
            waitUntilAcquired(filePath, out var fileStream, fileMode, fileAccess, fileShare, timeoutInMilliseconds, !noThrowOnTimeout);
            return fileStream;
        }

        /// <summary>
        /// Wait until file gets acquired lock but only as long the file stream is opened.
        /// </summary>
        /// <param name="filePath">The path to file that get locked.</param>
        /// <param name="fileStream">The locked file as file stream.</param>
        /// <param name="fileMode">The file mode when opening file.</param>
        /// <param name="fileAccess">The file access when opening file.</param>
        /// <param name="fileShare">The file share when opening file</param>
        /// <param name="throwOnTimeout">Enable throw when exception occured due due to timeout.</param>
        /// <returns>If true the lock acquirement was successful.</returns>
        public bool WaitUntilAcquired(string filePath, out FileStream? fileStream, FileMode fileMode = DefaultFileMode,
            FileAccess fileAccess = DefaultFileAccess, FileShare fileShare = DefaultFileShare, bool throwOnTimeout = false)
        {
            var timeoutInMilliseconds = DefaultTimeoutInMilliseconds;
            return waitUntilAcquired(filePath, out fileStream, fileMode, fileAccess, fileShare, timeoutInMilliseconds, throwOnTimeout);
        }

        /// <summary>
        /// Wait until file gets acquired lock but only as long the file stream is opened.
        /// </summary>
        /// <param name="filePath">The path to file that get locked.</param>
        /// <param name="fileMode">The file mode when opening file.</param>
        /// <param name="fileAccess">The file access when opening file.</param>
        /// <param name="fileShare">The file share when opening file</param>
        /// <param name="noThrowOnTimeout">Disable throw when exception occured due due to timeout.</param>
        /// <returns>If not null the lock acquirement was successful.</returns>
        public FileStream? WaitUntilAcquired(string filePath, FileMode fileMode = DefaultFileMode,
            FileAccess fileAccess = DefaultFileAccess, FileShare fileShare = DefaultFileShare, bool noThrowOnTimeout = false)
        {
            var timeoutInMilliseconds = DefaultTimeoutInMilliseconds;
            return waitUntilAcquired(filePath, fileMode, fileAccess, fileShare, timeoutInMilliseconds, noThrowOnTimeout);
        }

        /// <summary>
        /// Wait until file gets acquired lock but only as long the file stream is opened.
        /// </summary>
        /// <param name="filePath">The path to file that get locked.</param>
        /// <param name="timeoutInMilliseconds">The timeout in milliseconds.</param>
        /// <param name="fileStream">The locked file as file stream.</param>
        /// <param name="fileMode">The file mode when opening file.</param>
        /// <param name="fileAccess">The file access when opening file.</param>
        /// <param name="fileShare">The file share when opening file</param>
        /// <param name="throwOnTimeout">Enable throw when exception occured due due to timeout.</param>
        /// <returns>If true the lock acquirement was successful.</returns>
        public bool WaitUntilAcquired(string filePath, int timeoutInMilliseconds, out FileStream? fileStream, FileMode fileMode = DefaultFileMode,
            FileAccess fileAccess = DefaultFileAccess, FileShare fileShare = DefaultFileShare, bool throwOnTimeout = false) =>
            waitUntilAcquired(filePath, out fileStream, fileMode, fileAccess, fileShare, timeoutInMilliseconds, throwOnTimeout);

        /// <summary>
        /// Wait until file gets acquired lock but only as long the file stream is opened.
        /// </summary>
        /// <param name="filePath">The path to file that get locked.</param>
        /// <param name="timeoutInMilliseconds">The timeout in milliseconds.</param>
        /// <param name="fileMode">The file mode when opening file.</param>
        /// <param name="fileAccess">The file access when opening file.</param>
        /// <param name="fileShare">The file share when opening file</param>
        /// <param name="noThrowOnTimeout">Disable throw when exception occured due due to timeout.</param>
        /// <returns>If not null the lock acquirement was successful.</returns>
        public FileStream? WaitUntilAcquired(string filePath, int timeoutInMilliseconds, FileMode fileMode = DefaultFileMode,
            FileAccess fileAccess = DefaultFileAccess, FileShare fileShare = DefaultFileShare, bool noThrowOnTimeout = false) =>
            waitUntilAcquired(filePath, fileMode, fileAccess, fileShare, timeoutInMilliseconds, noThrowOnTimeout);

        /// <summary>
        /// Wait until file gets acquired lock but only as long the file stream is opened.
        /// </summary>
        /// <param name="filePath">The path to file that get locked.</param>
        /// <param name="timeout">The timeout specified as <see cref="TimeSpan"/>.</param>
        /// <param name="fileStream">The locked file as file stream.</param>
        /// <param name="fileMode">The file mode when opening file.</param>
        /// <param name="fileAccess">The file access when opening file.</param>
        /// <param name="fileShare">The file share when opening file</param>
        /// <param name="throwOnTimeout">Enable throw when exception occured due due to timeout.</param>
        /// <returns>If true the lock acquirement was successful.</returns>
        public bool WaitUntilAcquired(string filePath, TimeSpan timeout, out FileStream? fileStream, FileMode fileMode = DefaultFileMode,
            FileAccess fileAccess = DefaultFileAccess, FileShare fileShare = DefaultFileShare, bool throwOnTimeout = false)
        {
            var timeoutInMilliseconds = Convert.ToInt32(timeout.TotalMilliseconds);
            return waitUntilAcquired(filePath, out fileStream, fileMode, fileAccess, fileShare, timeoutInMilliseconds, throwOnTimeout);
        }

        /// <summary>
        /// Wait until file gets acquired lock but only as long the file stream is opened.
        /// </summary>
        /// <param name="filePath">The path to file that get locked.</param>
        /// <param name="timeout">The timeout specified as <see cref="TimeSpan"/>.</param>
        /// <param name="fileMode">The file mode when opening file.</param>
        /// <param name="fileAccess">The file access when opening file.</param>
        /// <param name="fileShare">The file share when opening file</param>
        /// <param name="noThrowOnTimeout">Disable throw when exception occured due due to timeout.</param>
        /// <returns>If ont null lock acquirement was successful.</returns>
        public FileStream? WaitUntilAcquired(string filePath, TimeSpan timeout, FileMode fileMode = DefaultFileMode,
            FileAccess fileAccess = DefaultFileAccess, FileShare fileShare = DefaultFileShare, bool noThrowOnTimeout = false)
        {
            var timeoutInMilliseconds = Convert.ToInt32(timeout.TotalMilliseconds);
            return waitUntilAcquired(filePath, fileMode, fileAccess, fileShare, timeoutInMilliseconds, noThrowOnTimeout);
        }
    }
}
