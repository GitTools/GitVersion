using GitVersion.Extensions;

namespace GitVersion.Configuration;

public interface IGitVersionConfiguration : IBranchConfiguration
{
    string? Workflow { get; }

    AssemblyVersioningScheme? AssemblyVersioningScheme { get; }

    AssemblyFileVersioningScheme? AssemblyFileVersioningScheme { get; }

    string? AssemblyInformationalFormat { get; }

    string? AssemblyVersioningFormat { get; }

    string? AssemblyFileVersioningFormat { get; }

    string? LabelPrefix { get; }

    string? NextVersion { get; }

    string? MajorVersionBumpMessage { get; }

    string? MinorVersionBumpMessage { get; }

    string? PatchVersionBumpMessage { get; }

    string? NoBumpMessage { get; }

    int? LabelPreReleaseWeight { get; }

    string? CommitDateFormat { get; }

    IReadOnlyDictionary<string, string> MergeMessageFormats { get; }

    bool UpdateBuildNumber { get; }

    SemanticVersionFormat SemanticVersionFormat { get; }

    IReadOnlyDictionary<string, BranchConfiguration> Branches { get; }

    IIgnoreConfiguration Ignore { get; }
}
