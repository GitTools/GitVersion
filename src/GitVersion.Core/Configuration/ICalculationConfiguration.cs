using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

/// <summary>Represents settings that participate in semantic-version calculation.</summary>
public interface ICalculationConfiguration : ICalculationBranchConfiguration
{
    /// <inheritdoc cref="IGitVersionConfiguration.Workflow"/>
    string? Workflow { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.TagPrefixPattern"/>
    string? TagPrefixPattern { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.VersionInBranchPattern"/>
    string? VersionInBranchPattern { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.NextVersion"/>
    string? NextVersion { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.MajorVersionBumpMessage"/>
    string? MajorVersionBumpMessage { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.MinorVersionBumpMessage"/>
    string? MinorVersionBumpMessage { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.PatchVersionBumpMessage"/>
    string? PatchVersionBumpMessage { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.NoBumpMessage"/>
    string? NoBumpMessage { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.VersionBumpResetMessage"/>
    string? VersionBumpResetMessage { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.MergeMessageFormats"/>
    IReadOnlyDictionary<string, string> MergeMessageFormats { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.SemanticVersionFormat"/>
    SemanticVersionFormat SemanticVersionFormat { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.VersionStrategy"/>
    VersionStrategies VersionStrategy { get; }

    /// <summary>Gets calculation settings for each configured branch.</summary>
    IReadOnlyDictionary<string, ICalculationBranchConfiguration> Branches { get; }

    /// <inheritdoc cref="IGitVersionConfiguration.Ignore"/>
    IIgnoreConfiguration Ignore { get; }
}
