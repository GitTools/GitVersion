using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;

namespace GitVersion;

internal class FileAppender : ILogAppender
{
    private readonly IFileSystem fileSystem;
    private readonly string filePath;

    public FileAppender(IFileSystem fileSystem, string filePath)
    {
        this.fileSystem = fileSystem.NotNull();
        this.filePath = filePath;

        var logFile = this.fileSystem.FileInfo.New(FileSystemHelper.Path.GetFullPath(filePath));

        logFile.Directory?.Create();
        if (logFile.Exists) return;

        using (logFile.CreateText()) { }
    }

    public void WriteTo(LogLevel level, string message)
    {
        try
        {
            WriteLogEntry(this.filePath, message);
        }
        catch
        {
            //
        }
    }

    private void WriteLogEntry(string logFilePath, string str)
    {
        var contents = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t\t{str}{FileSystemHelper.Path.NewLine}";
        this.fileSystem.File.AppendAllText(logFilePath, contents);
    }
}
