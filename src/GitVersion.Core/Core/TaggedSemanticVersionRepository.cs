using System.Collections.Concurrent;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Logging;

namespace GitVersion;

internal sealed class TaggedSemanticVersionRepository(ILogger<TaggedSemanticVersionRepository> logger, IRepositoryStore repositoryStore) : ITaggedSemanticVersionRepository
{
    private readonly ILogger<TaggedSemanticVersionRepository> logger = logger.NotNull();
    private readonly ConcurrentDictionary<(IBranch, string, SemanticVersionFormat), ILookup<ICommit, SemanticVersionWithTag>>
        taggedSemanticVersionsOfBranchCache = new();
    private readonly ConcurrentDictionary<(IBranch, string, SemanticVersionFormat), ILookup<ICommit, SemanticVersionWithTag>>
        taggedSemanticVersionsOfMergeTargetCache = new();
    private readonly ConcurrentDictionary<(string, SemanticVersionFormat), ILookup<ICommit, SemanticVersionWithTag>>
        taggedSemanticVersionsCache = new();

    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();

    /// <summary>
    /// Returns the semantic versions tagged on the commits of <paramref name="branch"/>, grouped by the commit
    /// they are tagged on and ordered by commit date, most recent first.
    /// </summary>
    public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfBranch(
       IBranch branch, string? tagPrefix, SemanticVersionFormat format, IIgnoreConfiguration ignore)
    {
        branch.NotNull();
        tagPrefix ??= string.Empty;

        var isCached = true;
        var result = this.taggedSemanticVersionsOfBranchCache.GetOrAdd(new(branch, tagPrefix, format), _ =>
        {
            isCached = false;
            return GetElements().Distinct().OrderByDescending(element => element.Tag.Commit.When)
                .ToLookup(element => element.Tag.Commit, element => element);
        });

        if (isCached)
        {
            this.logger.LogDebug(
                "Returning cached tagged semantic versions on branch '{BranchName}'. " +
                "TagPrefix: {TagPrefix} and Format: {Format}",
                branch.Name.Canonical, tagPrefix, format
            );
        }

        return result;

        IEnumerable<SemanticVersionWithTag> GetElements()
        {
            using (this.logger.StartIndentedScope($"Getting tagged semantic versions on branch '{branch.Name.Canonical}'. " +
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

    /// <summary>
    /// Returns the semantic versions tagged on commits which were merged into <paramref name="branch"/>, keyed by
    /// the commit on the branch which the tagged commit has as a parent.
    /// </summary>
    public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfMergeTarget(
        IBranch branch, string? tagPrefix, SemanticVersionFormat format, IIgnoreConfiguration ignore)
    {
        branch.NotNull();
        tagPrefix ??= string.Empty;

        var isCached = true;
        var result = this.taggedSemanticVersionsOfMergeTargetCache.GetOrAdd(new(branch, tagPrefix, format), _ =>
        {
            isCached = false;
            return GetElements().Distinct().OrderByDescending(element => element.Key.When)
                .ToLookup(element => element.Key, element => element.Value);
        });

        if (isCached)
        {
            this.logger.LogDebug(
                "Returning cached tagged semantic versions by track merge target '{BranchName}'. " +
                "TagPrefix: {TagPrefix} and Format: {Format}",
                branch.Name.Canonical, tagPrefix, format
            );
        }

        return result;

        IEnumerable<(ICommit Key, SemanticVersionWithTag Value)> GetElements()
        {
            using (this.logger.StartIndentedScope($"Getting tagged semantic versions by track merge target '{branch.Name.Canonical}'. " +
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

    /// <summary>
    /// Returns every tag in the repository whose name parses as a semantic version, grouped by the commit it
    /// points at and ordered by commit date, most recent first.
    /// </summary>
    public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersions(
        string? tagPrefix, SemanticVersionFormat format, IIgnoreConfiguration ignore)
    {
        tagPrefix ??= string.Empty;

        var isCached = true;
        var result = this.taggedSemanticVersionsCache.GetOrAdd(new(tagPrefix, format), _ =>
        {
            isCached = false;
            return GetElements().OrderByDescending(element => element.Tag.Commit.When)
                .ToLookup(element => element.Tag.Commit, element => element);
        });

        if (isCached)
        {
            this.logger.LogDebug("Returning cached tagged semantic versions. TagPrefix: {TagPrefix} and Format: {Format}", tagPrefix, format);
        }

        return result;

        IEnumerable<SemanticVersionWithTag> GetElements()
        {
            this.logger.LogInformation("Getting tagged semantic versions. TagPrefix: {TagPrefix} and Format: {Format}", tagPrefix, format);

            foreach (var tag in ignore.Filter(this.repositoryStore.Tags))
            {
                if (SemanticVersion.TryParse(tag.Name.Friendly, tagPrefix, out var semanticVersion, format))
                {
                    yield return new(semanticVersion, tag);
                }
            }
        }
    }
}
