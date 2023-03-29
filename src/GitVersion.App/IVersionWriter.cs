namespace GitVersion;

internal interface IVersionWriter
{
    void Write(Assembly assembly);
    void WriteTo(Assembly assembly, Action<string?> writeAction);
}
