using System;
using System.IO;

public class SelfCleaningDirectory
{
    public SelfCleaningDirectory(IPostTestDirectoryRemover directoryRemover, string path)
    {
        if (Directory.Exists(path))
        {
            throw new InvalidOperationException(string.Format("Directory '{0}' already exists.", path));
        }

        DirectoryPath = path;
        directoryRemover.Register(DirectoryPath);
    }

    public string DirectoryPath;
}