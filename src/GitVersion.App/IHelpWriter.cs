namespace GitVersion;

internal interface IHelpWriter
{
    void Write();
    void WriteTo(Action<string> writeAction);
}
