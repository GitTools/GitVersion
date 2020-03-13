using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GitVersion;
using GitVersion.Helpers;

namespace GitVersionCore.Tests.Helpers
{
    public class TestFileSystem : IFileSystem
    {
        private readonly Dictionary<string, byte[]> fileSystem = new Dictionary<string, byte[]>(StringComparerUtils.OsDependentComparer);

        public void Copy(string from, string to, bool overwrite)
        {
            var fromPath = Path.GetFullPath(from);
            var toPath = Path.GetFullPath(to);
            if (fileSystem.ContainsKey(toPath))
            {
                if (overwrite)
                    fileSystem.Remove(toPath);
                else
                    throw new IOException("File already exists");
            }

            if (!fileSystem.TryGetValue(fromPath, out var source))
                throw new FileNotFoundException($"The source file '{fromPath}' was not found", from);

            fileSystem.Add(toPath, source);
        }

        public void Move(string from, string to)
        {
            var fromPath = Path.GetFullPath(from);
            Copy(from, to, false);
            fileSystem.Remove(fromPath);
        }

        public bool Exists(string file)
        {
            var path = Path.GetFullPath(file);
            return fileSystem.ContainsKey(path);
        }

        public void Delete(string path)
        {
            var fullPath = Path.GetFullPath(path);
            fileSystem.Remove(fullPath);
        }

        public string ReadAllText(string file)
        {
            var path = Path.GetFullPath(file);
            if (!fileSystem.TryGetValue(path, out var content))
                throw new FileNotFoundException($"The file '{path}' was not found", path);

            var encoding = EncodingHelper.DetectEncoding(content) ?? Encoding.UTF8;
            return encoding.GetString(content);
        }

        public void WriteAllText(string file, string fileContents)
        {
            var path = Path.GetFullPath(file);
            var encoding = fileSystem.ContainsKey(path)
                ? EncodingHelper.DetectEncoding(fileSystem[path]) ?? Encoding.UTF8
                : Encoding.UTF8;
            WriteAllText(path, fileContents, encoding);
        }

        public void WriteAllText(string file, string fileContents, Encoding encoding)
        {
            var path = Path.GetFullPath(file);
            fileSystem[path] = encoding.GetBytes(fileContents);
        }

        public IEnumerable<string> DirectoryGetFiles(string directory, string searchPattern, SearchOption searchOption)
        {
            throw new NotImplementedException();
        }

        public Stream OpenWrite(string path)
        {
            return new TestStream(path, this);
        }

        public Stream OpenRead(string file)
        {
            var path = Path.GetFullPath(file);
            if (fileSystem.ContainsKey(path))
            {
                var content = fileSystem[path];
                return new MemoryStream(content);
            }

            throw new FileNotFoundException("File not found.", path);
        }

        public void CreateDirectory(string directory)
        {
            var path = Path.GetFullPath(directory);
            if (fileSystem.ContainsKey(path))
            {
                fileSystem[path] = new byte[0];
            }
            else
            {
                fileSystem.Add(path, new byte[0]);
            }
        }

        public bool DirectoryExists(string directory)
        {
            var path = Path.GetFullPath(directory);
            return fileSystem.ContainsKey(path);
        }

        public long GetLastDirectoryWrite(string path)
        {
            return 1;
        }

        public bool PathsEqual(string path, string otherPath)
        {
            return string.Equals(
                Path.GetFullPath(path).TrimEnd('\\').TrimEnd('/'),
                Path.GetFullPath(otherPath).TrimEnd('\\').TrimEnd('/'),
                StringComparerUtils.OsDependentComparison);
        }
    }
}
