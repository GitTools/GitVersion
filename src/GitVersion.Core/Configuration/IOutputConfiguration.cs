namespace GitVersion.Configuration;

/// <summary>Represents settings that affect assembly, build-server, and formatted version output.</summary>
public interface IOutputConfiguration : IOutputBranchConfiguration
{
    /// <inheritdoc cref="IGitVersionConfiguration.AssemblyVersioningScheme"/>
    AssemblyVersioningScheme? AssemblyVersioningScheme { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.AssemblyFileVersioningScheme"/>
    AssemblyFileVersioningScheme? AssemblyFileVersioningScheme { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.AssemblyInformationalFormat"/>
    string? AssemblyInformationalFormat { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.AssemblyVersioningFormat"/>
    string? AssemblyVersioningFormat { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.AssemblyFileVersioningFormat"/>
    string? AssemblyFileVersioningFormat { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.TagPreReleaseWeight"/>
    int? TagPreReleaseWeight { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.CommitDateFormat"/>
    string? CommitDateFormat { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.UpdateBuildNumber"/>
    bool UpdateBuildNumber { get; }

    /// <summary>Gets output settings for each configured branch.</summary>
    IReadOnlyDictionary<string, IOutputBranchConfiguration> Branches { get; }
}
