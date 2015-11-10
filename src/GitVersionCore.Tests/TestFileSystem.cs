using System;
using System.Collections.Generic;
using System.IO;

using GitVersion.Helpers;

using LibGit2Sharp;

public class TestFileSystem : IFileSystem
{
    Dictionary<string, string> fileSystem = new Dictionary<string, string>();


    public void Copy(string @from, string to, bool overwrite)
    {
        throw new NotImplementedException();
    }


    public void Move(string @from, string to)
    {
        throw new NotImplementedException();
    }


    public bool Exists(string file)
    {
        return fileSystem.ContainsKey(file);
    }


    public void Delete(string path)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }


    public void CreateDirectory(string path)
    {
        throw new NotImplementedException();
    }


    public long GetLastDirectoryWrite(string path)
    {
        throw new NotImplementedException();
    }


    public string TreeWalkForDotGitDir(string directory)
    {
        throw new NotImplementedException();
    }


    public IRepository GetRepository(string gitDirectory)
    {
        throw new NotImplementedException();
    }
}