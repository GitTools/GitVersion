namespace GitVersion.Helpers
{
    using System.Collections.Generic;
    using System.IO;

    using LibGit2Sharp;

    public interface IFileSystem
    {
        void Copy(string from, string to, bool overwrite);
        void Move(string from, string to);
        bool Exists(string file);
        void Delete(string path);
        string ReadAllText(string path);
        void WriteAllText(string file, string fileContents);
        IEnumerable<string> DirectoryGetFiles(string directory, string searchPattern, SearchOption searchOption);
        Stream OpenWrite(string path);
        Stream OpenRead(string path);
        void CreateDirectory(string path);
        long GetLastDirectoryWrite(string path);
        string TreeWalkForDotGitDir(string directory);
        Repository GetRepository(string gitDirectory);
    }
}