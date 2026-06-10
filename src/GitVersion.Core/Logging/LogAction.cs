namespace GitVersion.Logging;

/// <summary>A delegate that receives a <see cref="LogActionEntry"/> callback used to lazily compose a log message.</summary>
public delegate void LogAction(LogActionEntry actionEntry);

/// <summary>A delegate that formats and writes a log message using the supplied format string and arguments.</summary>
public delegate void LogActionEntry(string format, params object[] args);
