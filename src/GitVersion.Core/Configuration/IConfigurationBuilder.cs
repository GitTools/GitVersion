namespace GitVersion.Configuration;

internal interface IConfigurationBuilder
{
    void AddOverride(IReadOnlyDictionary<object, object?> value);

    IGitVersionConfiguration Build();
}
