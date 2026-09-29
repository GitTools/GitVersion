namespace GitVersion.Configuration;

internal interface IConfigurationSerializer
{
    T Deserialize<T>(string input);
    string Serialize(object graph);
    string SerializeProperty(IGitVersionConfiguration configuration, string path);
}
