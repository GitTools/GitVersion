using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GitVersion.Helpers;

namespace GitVersion.MSBuildTask.Tests.Helpers
{
    public class TestFileSystem : IFileSystem
    {
        private readonly Dictionary<string, byte[]> fileSystem = new Dictionary<string, byte[]>();

        public void Copy(string @from, string to, bool overwrite)
        {
            if (fileSystem.ContainsKey(to))
            {
                if (overwrite)
                    fileSystem.Remove(to);
                else
                    throw new IOException("File already exists");
            }

            if (!fileSystem.TryGetValue(from, out var source))
                throw new FileNotFoundException($"The source file '{@from}' was not found", from);

            fileSystem.Add(to, source);
        }

        public void Move(string @from, string to)
        {
            Copy(from, to, false);
            fileSystem.Remove(from);
        }

        public bool Exists(string file)
        {
            return fileSystem.ContainsKey(file);
        }

        public void Delete(string path)
        {
            fileSystem.Remove(path);
        }

        public string ReadAllText(string path)
        {
            if (!fileSystem.TryGetValue(path, out var content))
                throw new FileNotFoundException($"The file '{path}' was not found", path);

            var encoding = EncodingHelper.DetectEncoding(content) ?? Encoding.UTF8;
            return encoding.GetString(content);
        }

        public void WriteAllText(string file, string fileContents)
        {
            var encoding = fileSystem.ContainsKey(file)
                ? EncodingHelper.DetectEncoding(fileSystem[file]) ?? Encoding.UTF8
                : Encoding.UTF8;
            WriteAllText(file, fileContents, encoding);
        }

        public void WriteAllText(string file, string fileContents, Encoding encoding)
        {
            fileSystem[file] = encoding.GetBytes(fileContents);
        }

        public IEnumerable<string> DirectoryGetFiles(string directory, string searchPattern, SearchOption searchOption)
        {
            throw new NotImplementedException();
        }

        public Stream OpenWrite(string path)
        {
            return new TestStream(path, this);
        }

        public Stream OpenRead(string path)
        {
            if (fileSystem.ContainsKey(path))
            {
                var content = fileSystem[path];
                return new MemoryStream(content);
            }

            throw new FileNotFoundException("File not found.", path);
        }

        public void CreateDirectory(string path)
        {
            if (fileSystem.ContainsKey(path))
            {
                fileSystem[path] = new byte[0];
            }
            else
            {
                fileSystem.Add(path, new byte[0]);
            }
        }

        public bool DirectoryExists(string path)
        {
            return fileSystem.ContainsKey(path);
        }

        public long GetLastDirectoryWrite(string path)
        {
            return 1;
        }

        public bool PathsEqual(string path, string otherPath)
        {
            return path == otherPath;
        }
    }
}
