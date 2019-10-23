using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitVersion
{
    public interface IFileSystem
    {
        void Copy(string from, string to, bool overwrite);
        void Move(string from, string to);
        bool Exists(string file);
        void Delete(string path);
        string ReadAllText(string path);
        void WriteAllText(string file, string fileContents);
        void WriteAllText(string file, string fileContents, Encoding encoding);
        IEnumerable<string> DirectoryGetFiles(string directory, string searchPattern, SearchOption searchOption);
        Stream OpenWrite(string path);
        Stream OpenRead(string path);
        void CreateDirectory(string path);
        bool DirectoryExists(string path);
        long GetLastDirectoryWrite(string path);

        bool PathsEqual(string path, string otherPath);
    }
}
