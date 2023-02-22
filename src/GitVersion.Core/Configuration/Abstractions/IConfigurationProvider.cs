namespace GitVersion.Configuration;

public interface IConfigurationProvider
{
    GitVersionConfiguration Provide(IReadOnlyDictionary<object, object?>? overrideConfiguration = null);
    void Init(string workingDirectory);
}
