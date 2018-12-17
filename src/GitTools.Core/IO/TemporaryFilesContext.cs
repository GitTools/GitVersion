namespace GitTools
{
    using System;
    using System.IO;
    using Logging;

    public class TemporaryFilesContext : IDisposable
    {
        static readonly ILog Log = LogProvider.GetLogger(typeof(TemporaryFilesContext));
        private readonly Guid _randomGuid = Guid.NewGuid();
        private readonly string _rootDirectory;

        public TemporaryFilesContext()
        {
            _rootDirectory = Path.Combine(Path.GetTempPath(), "GitTools", _randomGuid.ToString());

            Directory.CreateDirectory(_rootDirectory);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Log.InfoFormat("Deleting temporary files from '{0}'", _rootDirectory);

            try
            {
                if (Directory.Exists(_rootDirectory))
                {
                    Directory.Delete(_rootDirectory, true);
                }
            }
            catch (Exception ex)
            {
                Log.ErrorException("Failed to delete temporary files", ex);
            }
        }

        public string GetDirectory(string relativeDirectoryName)
        {
            var fullPath = Path.Combine(_rootDirectory, relativeDirectoryName);

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            return fullPath;
        }

        public string GetFile(string relativeFilePath)
        {
            var fullPath = Path.Combine(_rootDirectory, relativeFilePath);

            var directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return fullPath;
        }
    }
}