using System.Collections.Concurrent;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Logging;

namespace GitVersion.Core;

internal sealed class TaggedSemanticVersionRepository(ILog log, IGitRepository gitRepository) : ITaggedSemanticVersionRepository
{
    private readonly ConcurrentDictionary<(IBranch, string, SemanticVersionFormat), IReadOnlyList<SemanticVersionWithTag>>
        taggedSemanticVersionsOfBranchCache = new();
    private readonly ConcurrentDictionary<(IBranch, string, SemanticVersionFormat), IReadOnlyList<(ICommit Key, SemanticVersionWithTag Value)>>
        taggedSemanticVersionsOfMergeTargetCache = new();
    private readonly ConcurrentDictionary<(string, SemanticVersionFormat), IReadOnlyList<SemanticVersionWithTag>>
        taggedSemanticVersionsCache = new();
    private readonly ILog log = log.NotNull();

    private readonly IGitRepository gitRepository = gitRepository.NotNull();

    public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfBranch(
       IBranch branch, string? tagPrefix, SemanticVersionFormat format, IIgnoreConfiguration ignore)
    {
        branch.NotNull();
        tagPrefix ??= string.Empty;

        bool isCached = true;
        var result = taggedSemanticVersionsOfBranchCache.GetOrAdd(new(branch, tagPrefix, format), _ =>
        {
            isCached = false;
            return GetElements().Distinct().OrderByDescending(element => element.Tag.Commit.When).ToList();
        });

        if (isCached)
        {
            this.log.Debug(
                $"Returning cached tagged semantic versions on branch '{branch.Name.Canonical}'. " +
                $"TagPrefix: {tagPrefix} and Format: {format}"
            );
        }

        return result.ToLookup(element => element.Tag.Commit, element => element);

        IEnumerable<SemanticVersionWithTag> GetElements()
        {
            using (this.log.IndentLog($"Getting tagged semantic versions on branch '{branch.Name.Canonical}'. " +
                                      $"TagPrefix: {tagPrefix} and Format: {format}"))
            {
                var semanticVersions = GetTaggedSemanticVersions(tagPrefix, format, ignore);

                foreach (var commit in ignore.Filter(branch.Commits))
                {
                    foreach (var semanticVersion in semanticVersions[commit])
                    {
                        yield return semanticVersion;
                    }
                }
            }
        }
    }

    public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfMergeTarget(
        IBranch branch, string? tagPrefix, SemanticVersionFormat format, IIgnoreConfiguration ignore)
    {
        branch.NotNull();
        tagPrefix ??= string.Empty;

        bool isCached = true;
        var result = taggedSemanticVersionsOfMergeTargetCache.GetOrAdd(new(branch, tagPrefix, format), _ =>
        {
            isCached = false;
            return GetElements().Distinct().OrderByDescending(element => element.Key.When).ToList();
        });

        if (isCached)
        {
            this.log.Debug(
                $"Returning cached tagged semantic versions by track merge target '{branch.Name.Canonical}'. " +
                $"TagPrefix: {tagPrefix} and Format: {format}"
            );
        }

        return result.ToLookup(element => element.Key, element => element.Value);

        IEnumerable<(ICommit Key, SemanticVersionWithTag Value)> GetElements()
        {
            using (this.log.IndentLog($"Getting tagged semantic versions by track merge target '{branch.Name.Canonical}'. " +
                                      $"TagPrefix: {tagPrefix} and Format: {format}"))
            {
                var shaHashSet = new HashSet<string>(ignore.Filter(branch.Commits).Select(element => element.Id.Sha));

                foreach (var semanticVersion in GetTaggedSemanticVersions(tagPrefix, format, ignore).SelectMany(v => v))
                {
                    foreach (var commit in semanticVersion.Tag.Commit.Parents.Where(element => shaHashSet.Contains(element.Id.Sha)))
                    {
                        yield return new(commit, semanticVersion);
                    }
                }
            }
        }
    }

    public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersions(
        string? tagPrefix, SemanticVersionFormat format, IIgnoreConfiguration ignore)
    {
        tagPrefix ??= string.Empty;

        bool isCached = true;
        var result = taggedSemanticVersionsCache.GetOrAdd(new(tagPrefix, format), _ =>
        {
            isCached = false;
            return GetElements().OrderByDescending(element => element.Tag.Commit.When).ToList();
        });

        if (isCached)
        {
            this.log.Debug($"Returning cached tagged semantic versions. TagPrefix: {tagPrefix} and Format: {format}");
        }

        return result.ToLookup(element => element.Tag.Commit, element => element);

        IEnumerable<SemanticVersionWithTag> GetElements()
        {
            this.log.Info($"Getting tagged semantic versions. TagPrefix: {tagPrefix} and Format: {format}");

            foreach (var tag in ignore.Filter(this.gitRepository.Tags))
            {
                if (SemanticVersion.TryParse(tag.Name.Friendly, tagPrefix, out var semanticVersion, format))
                {
                    yield return new(semanticVersion, tag);
                }
            }
        }
    }
}
