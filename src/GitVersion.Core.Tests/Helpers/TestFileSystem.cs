using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GitVersion.Helpers;

namespace GitVersion.Core.Tests.Helpers
{
    public class TestFileSystem : IFileSystem
    {
        private readonly Dictionary<string, byte[]> fileSystem = new(StringComparerUtils.OsDependentComparer);

        public void Copy(string from, string to, bool overwrite)
        {
            var fromPath = Path.GetFullPath(from);
            var toPath = Path.GetFullPath(to);
            if (this.fileSystem.ContainsKey(toPath))
            {
                if (overwrite)
                    this.fileSystem.Remove(toPath);
                else
                    throw new IOException("File already exists");
            }

            if (!this.fileSystem.TryGetValue(fromPath, out var source))
                throw new FileNotFoundException($"The source file '{fromPath}' was not found", from);

            this.fileSystem.Add(toPath, source);
        }

        public void Move(string from, string to)
        {
            var fromPath = Path.GetFullPath(from);
            Copy(from, to, false);
            this.fileSystem.Remove(fromPath);
        }

        public bool Exists(string file)
        {
            var path = Path.GetFullPath(file);
            return this.fileSystem.ContainsKey(path);
        }

        public void Delete(string path)
        {
            var fullPath = Path.GetFullPath(path);
            this.fileSystem.Remove(fullPath);
        }

        public string ReadAllText(string file)
        {
            var path = Path.GetFullPath(file);
            if (!this.fileSystem.TryGetValue(path, out var content))
                throw new FileNotFoundException($"The file '{path}' was not found", path);

            var encoding = EncodingHelper.DetectEncoding(content) ?? Encoding.UTF8;
            return encoding.GetString(content);
        }

        public void WriteAllText(string file, string fileContents)
        {
            var path = Path.GetFullPath(file);
            var encoding = this.fileSystem.ContainsKey(path)
                ? EncodingHelper.DetectEncoding(this.fileSystem[path]) ?? Encoding.UTF8
                : Encoding.UTF8;
            WriteAllText(path, fileContents, encoding);
        }

        public void WriteAllText(string file, string fileContents, Encoding encoding)
        {
            var path = Path.GetFullPath(file);
            this.fileSystem[path] = encoding.GetBytes(fileContents);
        }

        public IEnumerable<string> DirectoryEnumerateFiles(string directory, string searchPattern, SearchOption searchOption) => throw new NotImplementedException();

        public Stream OpenWrite(string path) => new TestStream(path, this);

        public Stream OpenRead(string file)
        {
            var path = Path.GetFullPath(file);
            if (this.fileSystem.ContainsKey(path))
            {
                var content = this.fileSystem[path];
                return new MemoryStream(content);
            }

            throw new FileNotFoundException("File not found.", path);
        }

        public void CreateDirectory(string directory)
        {
            var path = Path.GetFullPath(directory);
            if (this.fileSystem.ContainsKey(path))
            {
                this.fileSystem[path] = Array.Empty<byte>();
            }
            else
            {
                this.fileSystem.Add(path, Array.Empty<byte>());
            }
        }

        public bool DirectoryExists(string directory)
        {
            var path = Path.GetFullPath(directory);
            return this.fileSystem.ContainsKey(path);
        }

        public long GetLastDirectoryWrite(string path) => 1;

        public bool PathsEqual(string path, string otherPath) => string.Equals(
                Path.GetFullPath(path).TrimEnd('\\').TrimEnd('/'),
                Path.GetFullPath(otherPath).TrimEnd('\\').TrimEnd('/'),
                StringComparerUtils.OsDependentComparison);
    }
}
