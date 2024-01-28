using System.Collections.Concurrent;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.Core;

internal sealed class TaggedSemanticVersionRepository(
    ILog log,
    Lazy<GitVersionContext> versionContext,
    IGitRepository gitRepository,
    IBranchRepository branchRepository)
    : ITaggedSemanticVersionRepository
{
    private readonly ConcurrentDictionary<(IBranch, string, SemanticVersionFormat), ILookup<ICommit, SemanticVersionWithTag>>
        taggedSemanticVersionsOfBranchCache = new();
    private readonly ConcurrentDictionary<(IBranch, string, SemanticVersionFormat), ILookup<ICommit, SemanticVersionWithTag>>
        taggedSemanticVersionsOfMergeTargetCache = new();
    private readonly ConcurrentDictionary<(string, SemanticVersionFormat), ILookup<ICommit, SemanticVersionWithTag>>
        taggedSemanticVersionsCache = new();
    private readonly ILog log = log.NotNull();

    private GitVersionContext VersionContext => this.versionContextLazy.Value;
    private readonly Lazy<GitVersionContext> versionContextLazy = versionContext.NotNull();

    private readonly IGitRepository gitRepository = gitRepository.NotNull();
    private readonly IBranchRepository branchRepository = branchRepository.NotNull();

    public ILookup<ICommit, SemanticVersionWithTag> GetAllTaggedSemanticVersions(IBranch branch, EffectiveConfiguration configuration)
    {
        configuration.NotNull();

        IEnumerable<(ICommit Key, SemanticVersionWithTag Value)> GetElements()
        {
            var olderThan = VersionContext.CurrentCommit.When;

            var semanticVersionsOfBranch = GetTaggedSemanticVersionsOfBranch(
                branch: branch,
                tagPrefix: configuration.TagPrefix,
                format: configuration.SemanticVersionFormat
            );
            foreach (var grouping in semanticVersionsOfBranch)
            {
                if (grouping.Key.When > olderThan) continue;

                foreach (var semanticVersion in grouping)
                {
                    yield return new(grouping.Key, semanticVersion);
                }
            }

            if (configuration.TrackMergeTarget)
            {
                var semanticVersionsOfMergeTarget = GetTaggedSemanticVersionsOfMergeTarget(
                    branch: branch,
                    tagPrefix: configuration.TagPrefix,
                    format: configuration.SemanticVersionFormat
                );
                foreach (var grouping in semanticVersionsOfMergeTarget)
                {
                    if (grouping.Key.When > olderThan) continue;

                    foreach (var semanticVersion in grouping)
                    {
                        yield return new(grouping.Key, semanticVersion);
                    }
                }
            }

            if (configuration.TracksReleaseBranches)
            {
                var semanticVersionsOfReleaseBranches = GetTaggedSemanticVersionsOfReleaseBranches(
                    tagPrefix: configuration.TagPrefix,
                    format: configuration.SemanticVersionFormat,
                    excludeBranches: branch
                );
                foreach (var grouping in semanticVersionsOfReleaseBranches)
                {
                    if (grouping.Key.When > olderThan) continue;

                    foreach (var semanticVersion in grouping)
                    {
                        yield return new(grouping.Key, semanticVersion);
                    }
                }
            }

            if (!configuration.IsMainBranch && !configuration.IsReleaseBranch)
            {
                var semanticVersionsOfMainlineBranches = GetTaggedSemanticVersionsOfMainlineBranches(
                    tagPrefix: configuration.TagPrefix,
                    format: configuration.SemanticVersionFormat,
                    excludeBranches: branch
                );
                foreach (var grouping in semanticVersionsOfMainlineBranches)
                {
                    if (grouping.Key.When > olderThan) continue;

                    foreach (var semanticVersion in grouping)
                    {
                        yield return new(grouping.Key, semanticVersion);
                    }
                }
            }
        }

        return GetElements().Distinct().ToLookup(element => element.Key, element => element.Value);
    }

    public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfBranch(
        IBranch branch, string? tagPrefix, SemanticVersionFormat format)
    {
        branch.NotNull();
        tagPrefix ??= string.Empty;

        IEnumerable<SemanticVersionWithTag> GetElements()
        {
            using (this.log.IndentLog($"Getting tagged semantic versions on branch '{branch.Name.Canonical}'. " +
                $"TagPrefix: {tagPrefix} and Format: {format}"))
            {
                var semanticVersions = GetTaggedSemanticVersions(tagPrefix, format);
                foreach (var commit in branch.Commits)
                {
                    foreach (var semanticVersion in semanticVersions[commit])
                    {
                        yield return semanticVersion;
                    }
                }
            }
        }

        bool isCached = true;
        var result = taggedSemanticVersionsOfBranchCache.GetOrAdd(new(branch, tagPrefix, format), _ =>
        {
            isCached = false;
            return GetElements().Distinct().ToLookup(element => element.Tag.Commit, element => element);
        });

        if (isCached)
        {
            this.log.Debug(
                $"Returning cached tagged semantic versions on branch '{branch.Name.Canonical}'. " +
                $"TagPrefix: {tagPrefix} and Format: {format}"
            );
        }

        return result;
    }

    public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfMergeTarget(
        IBranch branch, string? tagPrefix, SemanticVersionFormat format)
    {
        branch.NotNull();
        tagPrefix ??= string.Empty;

        IEnumerable<(ICommit, SemanticVersionWithTag)> GetElements()
        {
            using (this.log.IndentLog($"Getting tagged semantic versions by track merge target '{branch.Name.Canonical}'. " +
                $"TagPrefix: {tagPrefix} and Format: {format}"))
            {
                var shaHashSet = new HashSet<string>(branch.Commits.Select(element => element.Id.Sha));

                foreach (var semanticVersion in GetTaggedSemanticVersions(tagPrefix, format).SelectMany(_ => _))
                {
                    foreach (var commit in semanticVersion.Tag.Commit.Parents.Where(element => shaHashSet.Contains(element.Id.Sha)))
                    {
                        yield return new(commit, semanticVersion);
                    }
                }
            }
        }

        bool isCached = true;
        var result = taggedSemanticVersionsOfMergeTargetCache.GetOrAdd(new(branch, tagPrefix, format), _ =>
        {
            isCached = false;
            return GetElements().Distinct().ToLookup(element => element.Item1, element => element.Item2);
        });

        if (isCached)
        {
            this.log.Debug(
                $"Returning cached tagged semantic versions by track merge target '{branch.Name.Canonical}'. " +
                $"TagPrefix: {tagPrefix} and Format: {format}"
            );
        }

        return result;
    }

    public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfMainlineBranches(
        string? tagPrefix, SemanticVersionFormat format, params IBranch[] excludeBranches)
    {
        tagPrefix ??= string.Empty;
        excludeBranches.NotNull();

        IEnumerable<SemanticVersionWithTag> GetElements()
        {
            using (this.log.IndentLog($"Getting tagged semantic versions of mainline branches. " +
                $"TagPrefix: {tagPrefix} and Format: {format}"))
            {
                foreach (var mainlinemBranch in branchRepository.GetMainBranches(excludeBranches))
                {
                    foreach (var semanticVersion in GetTaggedSemanticVersionsOfBranch(mainlinemBranch, tagPrefix, format).SelectMany(_ => _))
                    {
                        yield return semanticVersion;
                    }
                }
            }
        }

        return GetElements().Distinct().ToLookup(element => element.Tag.Commit, element => element);
    }

    public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfReleaseBranches(
        string? tagPrefix, SemanticVersionFormat format, params IBranch[] excludeBranches)
    {
        tagPrefix ??= string.Empty;
        excludeBranches.NotNull();

        IEnumerable<SemanticVersionWithTag> GetElements()
        {
            using (this.log.IndentLog($"Getting tagged semantic versions of release branches. " +
                $"TagPrefix: {tagPrefix} and Format: {format}"))
            {
                foreach (var releaseBranch in branchRepository.GetReleaseBranches(excludeBranches))
                {
                    foreach (var semanticVersion in GetTaggedSemanticVersionsOfBranch(releaseBranch, tagPrefix, format).SelectMany(_ => _))
                    {
                        yield return semanticVersion;
                    }
                }
            }
        }

        return GetElements().Distinct().ToLookup(element => element.Tag.Commit, element => element);
    }

    private ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersions(string? tagPrefix, SemanticVersionFormat format)
    {
        tagPrefix ??= string.Empty;

        IEnumerable<SemanticVersionWithTag> GetElements()
        {
            this.log.Info($"Getting tagged semantic versions. TagPrefix: {tagPrefix} and Format: {format}");

            foreach (var tag in this.gitRepository.Tags)
            {
                if (SemanticVersion.TryParse(tag.Name.Friendly, tagPrefix, out var semanticVersion, format))
                {
                    yield return new(semanticVersion, tag);
                }
            }
        }

        bool isCached = true;
        var result = taggedSemanticVersionsCache.GetOrAdd(new(tagPrefix, format), _ =>
        {
            isCached = false;
            return GetElements().ToLookup(element => element.Tag.Commit, element => element);
        });

        if (isCached)
        {
            this.log.Debug($"Returning cached tagged semantic versions. TagPrefix: {tagPrefix} and Format: {format}");
        }

        return result;
    }
}
