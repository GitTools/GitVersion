namespace GitVersion.Configuration;

internal interface IConfigurationSerializer
{
    T Deserialize<T>(string input);
    string Serialize(object graph);
    string SerializeDocument(IReadOnlyDictionary<object, object?> document);
}
