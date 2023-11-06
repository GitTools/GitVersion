using GitVersion.Helpers;
using GitVersion.Logging;

namespace GitVersion.Core.Tests.Helpers;

public class TestConsole : IConsole
{
    private readonly Queue<string> responses;
    private readonly ILog log;

    public TestConsole(params string[] responses)
    {
        this.log = new NullLog();
        this.responses = new Queue<string>(responses);
    }

    public void WriteLine(string? msg) => this.log.Info(msg + PathHelper.NewLine);

    public void WriteLine() => this.log.Info(PathHelper.NewLine);

    public void Write(string? msg) => this.log.Info(msg ?? throw new ArgumentNullException(nameof(msg)));

    public string ReadLine() => this.responses.Dequeue();

    public IDisposable UseColor(ConsoleColor consoleColor) => new NoOpDisposable();

    private class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
