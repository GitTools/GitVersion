using GitVersion.Helpers;

namespace GitVersion;

internal class ConsoleAdapter : IConsole
{
    public void WriteLine(string? msg) => Console.WriteLine(msg);

    public void Write(string? msg) => Console.Write(msg);

    public string? ReadLine() => Console.ReadLine();

    public IDisposable UseColor(ConsoleColor consoleColor)
    {
        var old = Console.ForegroundColor;
        Console.ForegroundColor = consoleColor;

        return Disposable.Create(() => Console.ForegroundColor = old);
    }
}
