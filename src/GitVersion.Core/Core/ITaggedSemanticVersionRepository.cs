using GitVersion.Configuration;

namespace GitVersion.Core;

internal interface ITaggedSemanticVersionRepository
{
    ILookup<ICommit, SemanticVersionWithTag> GetAllTaggedSemanticVersions(IBranch branch, EffectiveConfiguration configuration);

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfBranch(
        IBranch branch,
        string? tagPrefix,
        SemanticVersionFormat format);

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfMergeTarget(
        IBranch branch,
        string? tagPrefix,
        SemanticVersionFormat format);

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfMainBranches(
        string? tagPrefix,
        SemanticVersionFormat format,
        params IBranch[] excludeBranches);

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfReleaseBranches(
        string? tagPrefix,
        SemanticVersionFormat format,
        params IBranch[] excludeBranches);

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersions(string? tagPrefix, SemanticVersionFormat format);
}
