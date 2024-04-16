using GitVersion.Configuration;
using GitVersion.Git;

namespace GitVersion.Core;

internal interface ITaggedSemanticVersionService
{
    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersions(
        IBranch branch,
        IGitVersionConfiguration configuration,
        string? label,
        DateTimeOffset? notOlderThan,
        TaggedSemanticVersions taggedSemanticVersion);

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfBranch(
        IBranch branch,
        string? tagPrefix,
        SemanticVersionFormat format,
        IIgnoreConfiguration ignore,
        string? label = null,
        DateTimeOffset? notOlderThan = null);

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfMergeTarget(
        IBranch branch,
        string? tagPrefix,
        SemanticVersionFormat format,
        IIgnoreConfiguration ignore,
        string? label = null,
        DateTimeOffset? notOlderThan = null);

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfMainBranches(
        IGitVersionConfiguration configuration,
        DateTimeOffset? notOlderThan = null,
        string? label = null,
        params IBranch[] excludeBranches);

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfReleaseBranches(
        IGitVersionConfiguration configuration,
        DateTimeOffset? notOlderThan = null,
        string? label = null,
        params IBranch[] excludeBranches);
}
