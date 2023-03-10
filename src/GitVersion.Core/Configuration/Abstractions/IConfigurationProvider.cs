namespace GitVersion.Configuration;

public interface IConfigurationProvider
{
    IGitVersionConfiguration Provide(IReadOnlyDictionary<object, object?>? overrideConfiguration = null);
    void Init(string workingDirectory);
}
