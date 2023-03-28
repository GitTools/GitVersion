namespace GitVersion.Configuration;

public interface IConfigurationFileLocator
{
    bool TryGetConfigurationFile(string? workingDirectory, string? projectRootDirectory, out string? configFilePath);
    IGitVersionConfiguration ReadConfiguration(string? configFilePath);
    IReadOnlyDictionary<object, object?>? ReadOverrideConfiguration(string? configFilePath);
    void Verify(string? workingDirectory, string? projectRootDirectory);
}
