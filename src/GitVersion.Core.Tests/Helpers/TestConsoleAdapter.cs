using GitVersion.Helpers;

namespace GitVersion.Core.Tests.Helpers;

public class TestConsoleAdapter(StringBuilder sb) : IConsole
{
    public void WriteLine(string? msg) => sb.AppendLine(msg);

    public void Write(string? msg) => sb.Append(msg);

    public override string ToString() => sb.ToString();

    public string ReadLine() => throw new NotImplementedException();

    public IDisposable UseColor(ConsoleColor consoleColor) => Disposable.Empty;
}
