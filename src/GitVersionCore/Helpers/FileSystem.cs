namespace GitVersion.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using LibGit2Sharp;

    public class FileSystem : IFileSystem
    {
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

        public long GetLastDirectoryWrite(string path)
        {
            return new DirectoryInfo(path)
                .GetDirectories("*.*", SearchOption.AllDirectories)
                .Select(d => d.LastWriteTimeUtc)
                .DefaultIfEmpty()
                .Max()
                .Ticks;
        }

        public IRepository GetRepository(string gitDirectory)
        {
            try
            {
                var repository = new Repository(gitDirectory);

                var branch = repository.Head;
                if (branch.Tip == null)
                {
                    throw new WarningException("No Tip found. Has repo been initialized?");
                }
                return repository;
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains("LibGit2Sharp.Core.NativeMethods") || exception.Message.Contains("FilePathMarshaler"))
                {
                    throw new WarningException("Restart of the process may be required to load an updated version of LibGit2Sharp.");
                }
                throw;
            }
        }
    }
}