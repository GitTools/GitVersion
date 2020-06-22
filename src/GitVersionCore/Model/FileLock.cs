using GitVersion.Helpers.Abstractions;
using System;
using System.IO;

namespace GitVersion.Helpers
{
    public class FileLock : IFileLock
    {
        public FileStream FileStream { get; }

        public FileLock(FileStream fileStream) =>
            fileStream = fileStream ?? throw new ArgumentNullException(nameof(fileStream));

        public void Dispose() =>
            FileStream.Dispose();
    }
}
