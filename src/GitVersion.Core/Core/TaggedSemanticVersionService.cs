using System.Collections.Concurrent;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.Core;

internal interface IBranchRepository
{
    IEnumerable<IBranch> GetMainlineBranches(params IBranch[] excludeBranches);

    IEnumerable<IBranch> GetReleaseBranches(params IBranch[] excludeBranches);
}

internal sealed class BranchRepository : IBranchRepository
{
    private GitVersionContext VersionContext => this.versionContextLazy.Value;
    private readonly Lazy<GitVersionContext> versionContextLazy;

    private readonly IGitRepository gitRepository;

    public BranchRepository(Lazy<GitVersionContext> versionContext, IGitRepository gitRepository)
    {
        this.versionContextLazy = versionContext.NotNull();
        this.gitRepository = gitRepository.NotNull();
    }

    public IEnumerable<IBranch> GetMainlineBranches(params IBranch[] excludeBranches)
        => GetBranchesWhere(new HashSet<IBranch>(excludeBranches), configuration => configuration.IsMainline == true);

    public IEnumerable<IBranch> GetReleaseBranches(params IBranch[] excludeBranches)
        => GetBranchesWhere(new HashSet<IBranch>(excludeBranches), configuration => configuration.IsReleaseBranch == true);

    private IEnumerable<IBranch> GetBranchesWhere(HashSet<IBranch> excludeBranches, Func<IBranchConfiguration, bool> predicate)
    {
        predicate.NotNull();

        foreach (var branch in this.gitRepository.Branches)
        {
            if (!excludeBranches.Contains(branch))
            {
                var branchConfiguration = VersionContext.Configuration.GetBranchConfiguration(branch.Name);
                if (predicate(branchConfiguration))
                {
                    yield return branch;
                }
            }
        }
    }
}

internal interface ITaggedSemanticVersionRepository
{
    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersions(IBranch branch, EffectiveConfiguration configuration);

    ILookup<ICommit, SemanticVersionWithTag> GetAllTaggedSemanticVersions(string? tagPrefix, SemanticVersionFormat format);

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfBranch(
        IBranch branch, string? tagPrefix, SemanticVersionFormat format
    );

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfMergeTarget(
        IBranch branch, string? tagPrefix, SemanticVersionFormat format
    );

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfMainlineBranches(
        string? tagPrefix, SemanticVersionFormat format, params IBranch[] excludeBranches
    );

    ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersionsOfReleaseBranches(
        string? tagPrefix, SemanticVersionFormat format, params IBranch[] excludeBranches
    );
}

internal sealed class TaggedSemanticVersionRepository : ITaggedSemanticVersionRepository
{
    private readonly ILog log;

    private GitVersionContext VersionContext => this.versionContextLazy.Value;
    private readonly Lazy<GitVersionContext> versionContextLazy;

    private readonly IGitRepository gitRepository;
    private readonly IBranchRepository branchRepository;

    public TaggedSemanticVersionRepository(ILog log, Lazy<GitVersionContext> versionContext, IGitRepository gitRepository,
        IBranchRepository branchRepository)
    {
        this.log = log.NotNull();
        this.versionContextLazy = versionContext.NotNull();
        this.gitRepository = gitRepository.NotNull();
        this.branchRepository = branchRepository.NotNull();
    }

    public ILookup<ICommit, SemanticVersionWithTag> GetTaggedSemanticVersions(IBranch branch, EffectiveConfiguration configuration)
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

            if (!configuration.IsMainline && !configuration.IsReleaseBranch)
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

        return GetElements().ToLookup(element => element.Key, element => element.Value);
    }

    private readonly ConcurrentDictionary<(string, SemanticVersionFormat), ILookup<ICommit, SemanticVersionWithTag>>
        getAllTaggedSemanticVersionsCache = new();

    public ILookup<ICommit, SemanticVersionWithTag> GetAllTaggedSemanticVersions(string? tagPrefix, SemanticVersionFormat format)
    {
        tagPrefix ??= string.Empty;

        IEnumerable<SemanticVersionWithTag> GetElements()
        {
            this.log.Info($"Getting tagged semantic versions. TagPrefix: {tagPrefix} and Format: {format}");

            foreach (var tag in this.gitRepository.Tags)
            {
                if (SemanticVersion.TryParse(tag.Name.Friendly, tagPrefix, out var semanticVersion, format))
                {
                    yield return new SemanticVersionWithTag(semanticVersion, tag);
                }
            }
        }

        bool isCached = true;
        var result = getAllTaggedSemanticVersionsCache.GetOrAdd(new(tagPrefix, format), _ =>
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

    private readonly ConcurrentDictionary<(IBranch, string, SemanticVersionFormat), ILookup<ICommit, SemanticVersionWithTag>>
        getTaggedSemanticVersionsOfBranchCache = new();

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
                var semanticVersions = GetAllTaggedSemanticVersions(tagPrefix, format);

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
        var result = getTaggedSemanticVersionsOfBranchCache.GetOrAdd(new(branch, tagPrefix, format), _ =>
        {
            isCached = false;
            var semanticVersions = GetElements();
            return semanticVersions.ToLookup(element => element.Tag.Commit, element => element);
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

    private readonly ConcurrentDictionary<(IBranch, string, SemanticVersionFormat), ILookup<ICommit, SemanticVersionWithTag>>
        getTaggedSemanticVersionsOfMergeTargetCache = new();

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

                foreach (var semanticVersion in GetAllTaggedSemanticVersions(tagPrefix, format).SelectMany(_ => _))
                {
                    foreach (var commit in semanticVersion.Tag.Commit.Parents.Where(element => shaHashSet.Contains(element.Id.Sha)))
                    {
                        yield return new(commit, semanticVersion);
                    }
                }
            }
        }

        bool isCached = true;
        var result = getTaggedSemanticVersionsOfMergeTargetCache.GetOrAdd(new(branch, tagPrefix, format), _ =>
        {
            isCached = false;
            return GetElements().ToLookup(element => element.Item1, element => element.Item2);
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
                foreach (var mainlinemBranch in branchRepository.GetMainlineBranches(excludeBranches))
                {
                    foreach (var semanticVersion in GetTaggedSemanticVersionsOfBranch(mainlinemBranch, tagPrefix, format).SelectMany(_ => _))
                    {
                        yield return semanticVersion;
                    }
                }
            }
        }

        return GetElements().ToLookup(element => element.Tag.Commit, element => element);
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

        return GetElements().ToLookup(element => element.Tag.Commit, element => element);
    }
}
