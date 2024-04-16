using GitVersion.Configuration;
using GitVersion.Git;

namespace GitVersion.Core;

internal interface ITaggedSemanticVersionRepository
{
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

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersions(
        string? tagPrefix, SemanticVersionFormat format, IIgnoreConfiguration ignore);
}
