namespace GitVersion.Configuration;

internal interface IConfigurationSerializer
{
    public T Deserialize<T>(string input);
    string Serialize(object graph);
    public IGitVersionConfiguration? ReadConfiguration(string input);
}
