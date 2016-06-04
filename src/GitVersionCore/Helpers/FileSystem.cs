namespace GitVersion.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class FileSystem : IFileSystem
    {
        private static readonly bool runningOnMono = Type.GetType("Mono.Runtime") != null;

        public void Copy(string @from, string to, bool overwrite)
        {
            File.Copy(from, to, overwrite);
        }

        public void Move(string @from, string to)
        {
            File.Move(from, to);
        }

        public bool Exists(string file)
        {
            return File.Exists(file);
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public void WriteAllText(string file, string fileContents)
        {
            File.WriteAllText(file, fileContents);
        }

        public IEnumerable<string> DirectoryGetFiles(string directory, string searchPattern, SearchOption searchOption)
        {
            return Directory.GetFiles(directory, searchPattern, searchOption);
        }

        public Stream OpenWrite(string path)
        {
            return File.OpenWrite(path);
        }

        public Stream OpenRead(string path)
        {
            return File.OpenRead(path);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public long GetLastDirectoryWrite(string path)
        {
            return new DirectoryInfo(path)
                .GetDirectories("*.*", SearchOption.AllDirectories)
                .Select(d => d.LastWriteTimeUtc)
                .DefaultIfEmpty()
                .Max()
                .Ticks;
        }

        public bool PathsEqual(string path, string otherPath)
        {
            var comparison = runningOnMono
                ? StringComparison.InvariantCulture
                : StringComparison.InvariantCultureIgnoreCase;

            return string.Equals(
                Path.GetFullPath(path).TrimEnd('\\').TrimEnd('/'),
                Path.GetFullPath(otherPath).TrimEnd('\\').TrimEnd('/'),
                comparison);
        }
    }
}