namespace GitVersion.Logging;

/// <summary>Receives log messages forwarded by the <see cref="ILog"/> implementation, allowing custom sinks (e.g. file, build-agent output).</summary>
public interface ILogAppender
{
    /// <summary>Writes <paramref name="message"/> at the given <paramref name="level"/> to this appender's destination.</summary>
    void WriteTo(LogLevel level, string message);
}
