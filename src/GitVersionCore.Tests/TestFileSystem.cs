using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using GitVersion.Helpers;

public class TestFileSystem : IFileSystem
{
    Dictionary<string, string> fileSystem = new Dictionary<string, string>();

    public void Copy(string @from, string to, bool overwrite)
    {
        if (fileSystem.ContainsKey(to))
        {
            if (overwrite)
                fileSystem.Remove(to);
            else
                throw new IOException("File already exists");
        }

        string source;
        if (!fileSystem.TryGetValue(from, out source))
            throw new FileNotFoundException(string.Format("The source file '{0}' was not found", from), from);

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
        return fileSystem[path];
    }

    public void WriteAllText(string file, string fileContents)
    {
        if (fileSystem.ContainsKey(file))
        {
            fileSystem[file] = fileContents;
        }
        else
        {
            fileSystem.Add(file, fileContents);
        }
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
            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        throw new FileNotFoundException("File not found.", path);
    }

    public void CreateDirectory(string path)
    {
        if (fileSystem.ContainsKey(path))
        {
            fileSystem[path] = "";
        }
        else
        {
            fileSystem.Add(path, "");
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