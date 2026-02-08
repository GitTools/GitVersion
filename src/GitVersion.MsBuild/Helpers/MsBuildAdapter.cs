using GitVersion.Helpers;
using Microsoft.Build.Utilities;

namespace GitVersion.MsBuild;

internal class MsBuildAdapter(TaskLoggingHelper taskLog) : IConsole
{
    public void WriteLine(string? msg)
    {
        Write(msg);
        WriteLine();
    }

    public void Write(string? msg) => taskLog.LogMessage(msg);

    public string? ReadLine() => Console.ReadLine();

    public IDisposable UseColor(ConsoleColor consoleColor)
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = consoleColor;

        return Disposable.Create(() => Console.ForegroundColor = old);
    }

    private void WriteLine() => taskLog.LogMessage("\n");
}
