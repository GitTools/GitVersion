namespace GitVersion.Configuration;

/// <summary>Locates and validates the GitVersion configuration file on the file system.</summary>
public interface IConfigurationFileLocator
{
    /// <summary>Validates that at most one configuration file exists across the working and project root directories.</summary>
    void Verify(string? workingDirectory, string? projectRootDirectory);

    /// <summary>Returns the full path of the configuration file found in <paramref name="directoryPath"/>, or <see langword="null"/> if none exists.</summary>
    string? GetConfigurationFile(string? directoryPath);
}
