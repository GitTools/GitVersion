namespace GitVersion.Logging
{
    public delegate void LogAction(LogActionEntry actionEntry);

    public delegate void LogActionEntry(string format, params object[] args);
}
