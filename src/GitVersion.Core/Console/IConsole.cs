namespace GitVersion;

public interface IConsole
{
    void WriteLine(string? msg);
    void Write(string? msg);
    string? ReadLine();
    IDisposable UseColor(ConsoleColor consoleColor);
}
