using System.IO.Abstractions;

namespace GitVersion.Extensions;

public static class FileSystemExtensions
{
    public static long GetLastDirectoryWrite(this IFileSystem fileSystem, string path) => fileSystem.DirectoryInfo.New(path)
        .GetDirectories("*.*", SearchOption.AllDirectories)
        .Select(d => d.LastWriteTimeUtc)
        .DefaultIfEmpty()
        .Max()
        .Ticks;
}
