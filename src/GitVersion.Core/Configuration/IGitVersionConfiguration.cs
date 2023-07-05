using System.Text.RegularExpressions;
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

    string? TagPrefix { get; }

    string? VersionInBranchPattern { get; }

    Regex VersionInBranchRegex { get; }

    string? NextVersion { get; }

    string? MajorVersionBumpMessage { get; }

    string? MinorVersionBumpMessage { get; }

    string? PatchVersionBumpMessage { get; }

    string? NoBumpMessage { get; }

    int? TagPreReleaseWeight { get; }

    string? CommitDateFormat { get; }

    IReadOnlyDictionary<string, string> MergeMessageFormats { get; }

    bool UpdateBuildNumber { get; }

    SemanticVersionFormat SemanticVersionFormat { get; }

    IReadOnlyDictionary<string, IBranchConfiguration> Branches { get; }

    IIgnoreConfiguration Ignore { get; }

    string ToJsonString();
}
