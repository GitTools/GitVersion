namespace GitVersion.Logging;

/// <summary>Defines the severity levels used when writing log messages.</summary>
public enum LogLevel
{
    /// <summary>The application cannot continue and must terminate.</summary>
    Fatal,

    /// <summary>A serious failure occurred that may be recoverable.</summary>
    Error,

    /// <summary>An unexpected but non-fatal situation that deserves attention.</summary>
    Warn,

    /// <summary>General informational output about normal operation.</summary>
    Info,

    /// <summary>Detailed diagnostic output useful for troubleshooting.</summary>
    Verbose,

    /// <summary>Very detailed low-level diagnostic output, typically only useful during development.</summary>
    Debug
}
