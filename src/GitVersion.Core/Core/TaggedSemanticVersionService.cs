using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.Core;

internal sealed class TaggedSemanticVersionService(
    ITaggedSemanticVersionRepository Repository, IBranchRepository BranchRepository)
    : ITaggedSemanticVersionService
{
    private ITaggedSemanticVersionRepository Repository { get; } = Repository.NotNull();

    private IBranchRepository BranchRepository { get; } = BranchRepository.NotNull();

    public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersions(
         IBranch branch,
         IGitVersionConfiguration configuration,
         string? label,
         DateTimeOffset? notOlderThan,
         TaggedSemanticVersions taggedSemanticVersion)
    {
        IEnumerable<IEnumerable<KeyValuePair<ICommit, SemanticVersionWithTag>>> GetElements()
        {
            if (taggedSemanticVersion.HasFlag(TaggedSemanticVersions.OfBranch))
            {
                yield return GetTaggedSemanticVersionsOfBranchInternal(
                    branch: branch,
                    tagPrefix: configuration.TagPrefix,
                    format: configuration.SemanticVersionFormat,
                    ignore: configuration.Ignore,
                    label: label,
                    notOlderThan: notOlderThan
                );
            }

            if (taggedSemanticVersion.HasFlag(TaggedSemanticVersions.OfMergeTargets))
            {
                yield return GetTaggedSemanticVersionsOfMergeTargetInternal(
                    branch: branch,
                    tagPrefix: configuration.TagPrefix,
                    format: configuration.SemanticVersionFormat,
                    ignore: configuration.Ignore,
                    label: label,
                    notOlderThan: notOlderThan
                );
            }

            if (taggedSemanticVersion.HasFlag(TaggedSemanticVersions.OfMainBranches))
            {
                yield return GetTaggedSemanticVersionsOfMainBranchesInternal(
                    configuration: configuration,
                    label: label,
                    notOlderThan: notOlderThan,
                    excludeBranches: branch
                );
            }

            if (taggedSemanticVersion.HasFlag(TaggedSemanticVersions.OfReleaseBranches))
            {
                yield return GetTaggedSemanticVersionsOfReleaseBranchesInternal(
                    configuration: configuration,
                    label: label,
                    notOlderThan: notOlderThan,
                    excludeBranches: branch
                );
            }
        }

        return GetElements().SelectMany(elements => elements).Distinct()
            .OrderByDescending(element => element.Key.When)
            .ToLookup(element => element.Key, element => element.Value);
    }

    public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfBranch(
        IBranch branch,
        string? tagPrefix,
        SemanticVersionFormat format,
        IIgnoreConfiguration ignore,
        string? label,
        DateTimeOffset? notOlderThan)
    {
        var result = GetTaggedSemanticVersionsOfBranchInternal(
            branch: branch,
            tagPrefix: tagPrefix,
            format: format,
            ignore: ignore,
            label: label,
            notOlderThan: notOlderThan);

        return result.Distinct().OrderByDescending(element => element.Key.When)
            .ToLookup(element => element.Key, element => element.Value);
    }

    private IEnumerable<KeyValuePair<ICommit, SemanticVersionWithTag>> GetTaggedSemanticVersionsOfBranchInternal(
        IBranch branch,
        string? tagPrefix,
        SemanticVersionFormat format,
        IIgnoreConfiguration ignore,
        string? label,
        DateTimeOffset? notOlderThan)
    {
        var semanticVersionsOfBranch = Repository.GetTaggedSemanticVersionsOfBranch(
            branch: branch, tagPrefix: tagPrefix, format: format, ignore: ignore
        );
        foreach (var grouping in semanticVersionsOfBranch)
        {
            if (grouping.Key.When > notOlderThan) continue;

            foreach (var semanticVersion in grouping)
            {
                if (semanticVersion.Value.IsMatchForBranchSpecificLabel(label))
                {
                    yield return new(grouping.Key, semanticVersion);
                }
            }
        }
    }

    public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfMergeTarget(
         IBranch branch,
         string? tagPrefix,
         SemanticVersionFormat format,
         IIgnoreConfiguration ignore,
         string? label,
         DateTimeOffset? notOlderThan)
    {
        var result = GetTaggedSemanticVersionsOfMergeTargetInternal(
            branch: branch,
            tagPrefix: tagPrefix,
            format: format,
            ignore: ignore,
            label: label,
            notOlderThan: notOlderThan);

        return result.Distinct()
            .OrderByDescending(element => element.Key.When)
            .ToLookup(element => element.Key, element => element.Value);
    }

    private IEnumerable<KeyValuePair<ICommit, SemanticVersionWithTag>> GetTaggedSemanticVersionsOfMergeTargetInternal(
         IBranch branch,
         string? tagPrefix,
         SemanticVersionFormat format,
         IIgnoreConfiguration ignore,
         string? label,
         DateTimeOffset? notOlderThan)
    {
        var semanticVersionsOfMergeTarget = Repository.GetTaggedSemanticVersionsOfMergeTarget(
            branch: branch,
            tagPrefix: tagPrefix,
            format: format,
            ignore: ignore
        );
        foreach (var grouping in semanticVersionsOfMergeTarget)
        {
            if (grouping.Key.When > notOlderThan) continue;

            foreach (var semanticVersion in grouping)
            {
                if (semanticVersion.Value.IsMatchForBranchSpecificLabel(label))
                {
                    yield return new(grouping.Key, semanticVersion);
                }
            }
        }
    }

    public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfMainBranches(
        IGitVersionConfiguration configuration,
        DateTimeOffset? notOlderThan,
        string? label,
        params IBranch[] excludeBranches)
    {
        var result = GetTaggedSemanticVersionsOfMainBranchesInternal(
            configuration: configuration,
            notOlderThan: notOlderThan,
            label: label,
            excludeBranches: excludeBranches);

        return result.Distinct()
            .OrderByDescending(element => element.Key.When)
            .ToLookup(element => element.Key, element => element.Value);
    }

    private IEnumerable<KeyValuePair<ICommit, SemanticVersionWithTag>> GetTaggedSemanticVersionsOfMainBranchesInternal(
        IGitVersionConfiguration configuration,
        DateTimeOffset? notOlderThan,
        string? label,
        params IBranch[] excludeBranches)
    {
        foreach (var releaseBranch in BranchRepository.GetMainBranches(configuration, excludeBranches))
        {
            var taggedSemanticVersions = GetTaggedSemanticVersionsOfBranchInternal(
                branch: releaseBranch,
                tagPrefix: configuration.TagPrefix,
                format: configuration.SemanticVersionFormat,
                ignore: configuration.Ignore,
                label: label,
                notOlderThan: notOlderThan);

            foreach (var semanticVersion in taggedSemanticVersions)
            {
                yield return semanticVersion;
            }
        }
    }

    public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfReleaseBranches(
        IGitVersionConfiguration configuration,
        DateTimeOffset? notOlderThan,
        string? label,
        params IBranch[] excludeBranches)
    {
        var result = GetTaggedSemanticVersionsOfReleaseBranchesInternal(
            configuration: configuration,
            notOlderThan: notOlderThan,
            label: label,
            excludeBranches: excludeBranches);

        return result.Distinct()
            .OrderByDescending(element => element.Key.When)
            .ToLookup(element => element.Key, element => element.Value);
    }

    private IEnumerable<KeyValuePair<ICommit, SemanticVersionWithTag>> GetTaggedSemanticVersionsOfReleaseBranchesInternal(
        IGitVersionConfiguration configuration,
        DateTimeOffset? notOlderThan,
        string? label,
        params IBranch[] excludeBranches)
    {
        foreach (var releaseBranch in BranchRepository.GetReleaseBranches(configuration, excludeBranches))
        {
            var taggedSemanticVersions = GetTaggedSemanticVersionsOfBranchInternal(
                branch: releaseBranch,
                tagPrefix: configuration.TagPrefix,
                format: configuration.SemanticVersionFormat,
                ignore: configuration.Ignore,
                label: label,
                notOlderThan: notOlderThan);

            foreach (var semanticVersion in taggedSemanticVersions)
            {
                yield return semanticVersion;
            }
        }
    }
}
