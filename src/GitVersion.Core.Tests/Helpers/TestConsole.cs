using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;

namespace GitVersion.Core.Tests.Helpers;

public class TestConsole(params string[] responses) : IConsole
{
    private readonly Queue<string> responses = new(responses);
    private readonly ILog log = new NullLog();

    public void WriteLine(string? msg) => this.log.Info(msg + FileSystemHelper.Path.NewLine);

    public void WriteLine() => this.log.Info(FileSystemHelper.Path.NewLine);

    public void Write(string? msg) => this.log.Info(msg.NotNull());

    public string ReadLine() => this.responses.Dequeue();

    public IDisposable UseColor(ConsoleColor consoleColor) => new NoOpDisposable();

    private class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
