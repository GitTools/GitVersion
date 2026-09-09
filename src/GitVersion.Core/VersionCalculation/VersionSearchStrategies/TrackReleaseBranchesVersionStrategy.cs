using System.Diagnostics.CodeAnalysis;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

internal sealed class TrackReleaseBranchesVersionStrategy(
    Lazy<GitVersionContext> contextLazy,
    IRepositoryStore repositoryStore,
    IBranchRepository branchRepository,
    ITaggedSemanticVersionRepository taggedSemanticVersionRepository,
    IIncrementStrategyFinder incrementStrategyFinder,
    IEnvironment environment)
    : IVersionStrategy
{
    private readonly Lazy<GitVersionContext> contextLazy = contextLazy.NotNull();
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly IBranchRepository branchRepository = branchRepository.NotNull();
    private readonly ITaggedSemanticVersionRepository taggedSemanticVersionRepository = taggedSemanticVersionRepository.NotNull();
    private readonly IIncrementStrategyFinder incrementStrategyFinder = incrementStrategyFinder.NotNull();
    private readonly IEnvironment environment = environment.NotNull();
    private readonly VersionInBranchNameVersionStrategy releaseVersionStrategy = new(contextLazy, environment);

    private GitVersionContext Context => this.contextLazy.Value;

    public IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
    {
        configuration.NotNull();

        if (!Context.Configuration.VersionStrategy.HasFlag(VersionStrategies.TrackReleaseBranches))
        {
            yield break;
        }

        if (!configuration.Value.TracksReleaseBranches)
        {
            yield break;
        }

        var mainCommits = new Lazy<HashSet<ICommit>>(GetMainCommits);
        foreach (var releaseBranch in this.branchRepository.GetReleaseBranches(Context.Configuration))
        {
            if (TryGetBaseVersion(releaseBranch, configuration, mainCommits, out var baseVersion))
            {
                yield return baseVersion;
            }
        }
    }

    private bool TryGetBaseVersion(
        IBranch releaseBranch, EffectiveBranchConfiguration configuration, Lazy<HashSet<ICommit>> mainCommits,
        [NotNullWhen(true)] out BaseVersion? result)
    {
        result = null;

        var releaseBranchConfiguration = Context.Configuration.GetEffectiveBranchConfiguration(releaseBranch);
        if (!this.releaseVersionStrategy.TryGetBaseVersion(releaseBranchConfiguration, out var baseVersion)
            && !TryGetTaggedBaseVersion(releaseBranchConfiguration, mainCommits, out baseVersion))
        {
            return result is not null;
        }
        // Find the commit where the child branch was created.
        var baseVersionSource = this.repositoryStore.FindMergeBase(releaseBranch, Context.CurrentBranch);
        if (baseVersionSource is null)
        {
            return false;
        }

        var label = configuration.Value.GetBranchSpecificLabel(Context.CurrentBranch.Name, null, this.environment);
        var increment = this.incrementStrategyFinder.DetermineIncrementedField(
            currentCommit: Context.CurrentCommit,
            baseVersionSource: baseVersionSource,
            shouldIncrement: true,
            configuration: configuration.Value,
            label: label
        );

        result = new BaseVersion(
            "Release branch exists -> " + baseVersion.Source, baseVersion.SemanticVersion, baseVersionSource)
        {
            Operator = new BaseVersionOperator
            {
                Increment = increment,
                ForceIncrement = false,
                Label = label
            }
        };

        return true;
    }

    private bool TryGetTaggedBaseVersion(
        EffectiveBranchConfiguration configuration, Lazy<HashSet<ICommit>> mainCommits,
        [NotNullWhen(true)] out BaseVersion? result)
    {
        result = null;
        var releaseBranch = configuration.Branch;
        var baseVersionSource = releaseBranch.Tip is null
            ? null : this.repositoryStore.FindMergeBase(releaseBranch.Tip, Context.CurrentCommit);
        if (baseVersionSource is null)
        {
            return false;
        }

        var label = configuration.Value.GetBranchSpecificLabel(releaseBranch.Name, null, this.environment);
        var tags = this.taggedSemanticVersionRepository.GetTaggedSemanticVersionsOfBranch(
            releaseBranch, configuration.Value.TagPrefixPattern, configuration.Value.SemanticVersionFormat,
            Context.Configuration.Ignore);

        // Exclude tags inherited from main's first-parent history, but preserve release
        // tags subsequently brought into main by a merge. Ignore merged side branches
        // when determining the release's own history as well.
        var mainCommitSet = mainCommits.Value;
        var releaseCommits = releaseBranch.Commits.QueryBy(new CommitFilter
        {
            IncludeReachableFrom = releaseBranch,
            FirstParentOnly = true
        }).Where(commit => !mainCommitSet.Contains(commit)).ToHashSet();

        // Like versioned release names, tags describe the current release refs even
        // when calculating an older commit. Advancing develop must not change eligibility.
        var tag = tags.Where(group => releaseCommits.Contains(group.Key))
            .SelectMany(group => group)
            .Where(candidate => candidate.Value.IsPreRelease && candidate.Value.IsMatchForBranchSpecificLabel(label))
            .MaxBy(candidate => candidate.Value);
        if (tag is null)
        {
            return false;
        }

        // Treat the release's target as a stable version so the normal develop increment
        // advances the minor instead of only changing the prerelease label or number.
        var version = tag.Value;
        result = new BaseVersion($"Version in release branch tag '{tag.Tag.Name.Friendly}'",
            new SemanticVersion(version.Major, version.Minor, version.Patch));
        return true;
    }

    private HashSet<ICommit> GetMainCommits() => [.. this.branchRepository.GetMainBranches(Context.Configuration)
        .SelectMany(branch => branch.Commits.QueryBy(new CommitFilter
        {
            IncludeReachableFrom = branch,
            FirstParentOnly = true
        }))];
}
