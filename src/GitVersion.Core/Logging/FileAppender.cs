using GitVersion.Helpers;

namespace GitVersion.Logging;

internal class FileAppender : ILogAppender
{
    private readonly string filePath;

    public FileAppender(string filePath)
    {
        this.filePath = filePath;

        var logFile = new FileInfo(Path.GetFullPath(filePath));

        // NOTE: logFile.Directory will be null if the path is i.e. C:\logfile.log. @asbjornu
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

    private static void WriteLogEntry(string logFilePath, string str)
    {
        var contents = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t\t{str}{PathHelper.NewLine}";
        File.AppendAllText(logFilePath, contents);
    }
}
