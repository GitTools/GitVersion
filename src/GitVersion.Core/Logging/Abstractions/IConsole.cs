namespace GitVersion.Logging;

/// <summary>Provides console I/O operations used by GitVersion's logging infrastructure.</summary>
public interface IConsole
{
    /// <summary>Writes <paramref name="msg"/> followed by a newline to the console.</summary>
    void WriteLine(string? msg);

    /// <summary>Writes <paramref name="msg"/> to the console without a trailing newline.</summary>
    void Write(string? msg);

    /// <summary>Reads a line of text from the console.</summary>
    string? ReadLine();

    /// <summary>Returns a disposable that sets the console foreground colour to <paramref name="consoleColor"/> and restores the previous colour when disposed.</summary>
    IDisposable UseColor(ConsoleColor consoleColor);
}
