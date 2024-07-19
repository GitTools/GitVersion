namespace GitVersion.Configuration;

public interface IConfigurationBuilder
{
    void AddOverride(IReadOnlyDictionary<object, object?> value);

    IGitVersionConfiguration Build();
}
