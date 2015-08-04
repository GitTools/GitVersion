using System.IO;
using System.Linq;

public static class DirectoryDateFinder
{
    public static long GetLastDirectoryWrite(string path)
    {
        return new DirectoryInfo(path)
            .GetDirectories("*.*", SearchOption.AllDirectories)
            .Select(d => d.LastWriteTimeUtc)
            .DefaultIfEmpty()
            .Max()
            .Ticks;
    }
}