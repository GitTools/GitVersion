using System;
using System.IO;

public static class DirectoryDateFinder
{
    public static DateTime GetLastDirectoryWrite(string path)
    {
        var lastHigh = DateTime.MinValue;
        foreach (var file in Directory.EnumerateDirectories(path, "*.*", SearchOption.AllDirectories))
        {
            var lastWriteTime = File.GetLastWriteTime(file);
            if (lastWriteTime > lastHigh)
            {
                lastHigh = lastWriteTime;
            }
        }
        return lastHigh;
    }
}