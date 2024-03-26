namespace GitVersion.Configuration;

public interface IConfigurationFileLocator
{
    void Verify(string? workingDirectory, string? projectRootDirectory);
    string? GetConfigurationFile(string? directory);
}
