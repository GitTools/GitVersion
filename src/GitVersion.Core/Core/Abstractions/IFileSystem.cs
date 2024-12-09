namespace GitVersion;

public interface IFileSystem
{
    void Copy(string from, string to, bool overwrite);
    void Move(string from, string to);
    bool Exists(string file);
    void Delete(string path);
    string ReadAllText(string path);
    void WriteAllText(string? file, string fileContents);
    void WriteAllText(string? file, string fileContents, Encoding encoding);
    Stream OpenWrite(string path);
    Stream OpenRead(string path);
    void CreateDirectory(string path);
    bool DirectoryExists(string path);
    string[] GetFiles(string path);
    string[] GetDirectories(string path);
    IEnumerable<string> DirectoryEnumerateFiles(string? directory, string searchPattern, SearchOption searchOption);
    long GetLastDirectoryWrite(string path);
    long GetLastWriteTime(string path);
}
