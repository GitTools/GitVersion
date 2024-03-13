using GitVersion.Configuration;

namespace GitVersion.Core;

internal interface ITaggedSemanticVersionRepository
{
    ILookup<ICommit, SemanticVersionWithTag> GetAllTaggedSemanticVersions(
        IGitVersionConfiguration configuration,
        EffectiveConfiguration effectiveConfiguration,
        IBranch branch,
        string? label,
        DateTimeOffset? notOlderThan);

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfBranch(
        IBranch branch,
        string? tagPrefix,
        SemanticVersionFormat format,
        IIgnoreConfiguration ignore);

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfMergeTarget(
        IBranch branch,
        string? tagPrefix,
        SemanticVersionFormat format,
        IIgnoreConfiguration ignore);

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfMainBranches(
        IGitVersionConfiguration configuration,
        string? tagPrefix,
        SemanticVersionFormat format,
        params IBranch[] excludeBranches);

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfReleaseBranches(
        IGitVersionConfiguration configuration,
        string? tagPrefix,
        SemanticVersionFormat format,
        params IBranch[] excludeBranches);

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersions(
        string? tagPrefix, SemanticVersionFormat format, IIgnoreConfiguration ignore);
}
