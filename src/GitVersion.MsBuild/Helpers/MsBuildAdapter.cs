using GitVersion.Logging;
using Microsoft.Build.Utilities;

namespace GitVersion.MsBuild;

internal class MsBuildAdapter : IConsole
{
    private readonly TaskLoggingHelper taskLog;

    public MsBuildAdapter(TaskLoggingHelper taskLog) => this.taskLog = taskLog;

    public void WriteLine(string msg)
    {
        Write(msg);
        WriteLine();
    }

    public void WriteLine() => this.taskLog.LogMessage("\n");

    public void Write(string msg) => this.taskLog.LogMessage(msg);

    public string ReadLine() => Console.ReadLine();

    public IDisposable UseColor(ConsoleColor consoleColor)
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = consoleColor;

        return Disposable.Create(() => Console.ForegroundColor = old);
    }
}
