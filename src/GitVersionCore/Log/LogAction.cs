namespace GitVersion.Log
{
    public delegate void LogAction(LogActionEntry actionEntry);

    public delegate void LogActionEntry(string format, params object[] args);
}
