using System.ComponentModel;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.VersionCalculation.TrunkBased.NonTrunk;
using GitVersion.VersionCalculation.TrunkBased.Trunk;

namespace GitVersion.VersionCalculation.TrunkBased;

internal sealed class TrunkBasedVersionStrategy : VersionStrategyBase
{
    private volatile int IterationCounter;
    private IReadOnlyDictionary<string, HashSet<SemanticVersion>> taggedSemanticVersions;

    private IRepositoryStore RepositoryStore { get; }

    private IIncrementStrategyFinder IncrementStrategyFinder { get; }

    public TrunkBasedVersionStrategy(Lazy<GitVersionContext> context, IRepositoryStore repositoryStore,
        IIncrementStrategyFinder incrementStrategyFinder) : base(context)
    {
        RepositoryStore = repositoryStore.NotNull();
        IncrementStrategyFinder = incrementStrategyFinder.NotNull();
    }

    public override IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
    {
        if (Context.Configuration.VersioningMode != VersioningMode.TrunkBased) yield break;

        InitializeTaggedSemanticVersions(configuration);

        var iteration = CreateIteration(branchName: Context.CurrentBranch.Name, configuration: configuration.Value);

        var targetLabel = configuration.Value.GetBranchSpecificLabel(Context.CurrentBranch.Name, null);
        IterateOverCommitsRecursive(
            commitsInReverseOrder: Context.CurrentBranch.Commits,
            iteration: iteration,
            targetLabel: targetLabel
        );

        yield return DetermineBaseVersion(iteration, targetLabel, taggedSemanticVersions);
    }

    private void InitializeTaggedSemanticVersions(EffectiveBranchConfiguration configuration)
    {
        Dictionary<string, HashSet<SemanticVersion>> dictionary = new();

        var semanticVersions = RepositoryStore.GetSemanticVersions(
            configuration: Context.Configuration,
            currentBranch: Context.CurrentBranch,
            currentCommit: Context.CurrentCommit,
            trackMergeTarget: configuration.Value.TrackMergeTarget,
            tracksReleaseBranches: configuration.Value.TracksReleaseBranches
        ).ToArray();

        foreach (var semanticVersionsGrouping in semanticVersions.GroupBy(element => element.Tag.Commit.Sha))
        {
            HashSet<SemanticVersion> value = new(semanticVersionsGrouping.Select(element => element.Value));
            dictionary.Add(semanticVersionsGrouping.Key, value);
        }
        taggedSemanticVersions = dictionary;
    }

    private TrunkBasedIteration CreateIteration(
        ReferenceName branchName, EffectiveConfiguration configuration, TrunkBasedIteration? parent = null)
    {
        var iterationCount = Interlocked.Increment(ref IterationCounter);
        return new TrunkBasedIteration(
            id: $"#{iterationCount}",
            branchName: branchName,
            configuration: configuration,
            parent: parent
        );
    }

    private bool IterateOverCommitsRecursive(IEnumerable<ICommit> commitsInReverseOrder, TrunkBasedIteration iteration,
        string? targetLabel, HashSet<ICommit>? traversedCommits = null)
    {
        traversedCommits ??= new HashSet<ICommit>();

        Lazy<IReadOnlyDictionary<ICommit, EffectiveBranchConfiguration>> commitsWasBranchedFromLazy = new(
            () => GetCommitsWasBranchedFrom(branchName: iteration.BranchName)
        );

        var configuration = iteration.Configuration;
        var branchName = iteration.BranchName;

        foreach (var item in commitsInReverseOrder)
        {
            if (!traversedCommits.Add(item)) continue;

            if (commitsWasBranchedFromLazy.Value.TryGetValue(item, out var effectiveConfigurationWasBranchedFrom)
                && (!configuration.IsMainline || effectiveConfigurationWasBranchedFrom.Value.IsMainline))
            {
                configuration = effectiveConfigurationWasBranchedFrom.Value;
                branchName = effectiveConfigurationWasBranchedFrom.Branch.Name;
            }

            var incrementForcedByCommit = GetIncrementForcedByCommit(item, configuration);
            var commit = iteration.CreateCommit(item, branchName, configuration, incrementForcedByCommit);

            if (taggedSemanticVersions.TryGetValue(item.Sha, out var values))
            {
                commit.AddSemanticVersions(values);

                var label = targetLabel ?? configuration.GetBranchSpecificLabel(branchName, null);
                foreach (var semanticVersion in values)
                {
                    if (semanticVersion.IsMatchForBranchSpecificLabel(label)) return true;
                }
            }

            if (item.IsMergeCommit)
            {
                Lazy<IReadOnlyCollection<ICommit>> mergedCommitsInReverseOrderLazy = new(
                    () => IncrementStrategyFinder.GetMergedCommits(item, 1).Reverse().ToList()
                );

                if (configuration.TrackMergeMessage
                    && MergeMessage.TryParse(out var mergeMessage, item, Context.Configuration))
                {
                    if (mergeMessage.Version is not null)
                    {
                        commit.AddSemanticVersions(mergeMessage.Version);
                        return true;
                    }

                    if (mergeMessage.MergedBranch is not null)
                    {
                        var childConfiguration = Context.Configuration.GetEffectiveConfiguration(
                            mergeMessage.MergedBranch
                        );

                        if (childConfiguration.IsMainline)
                        {
                            if (configuration.IsMainline) throw new NotImplementedException();
                            mergedCommitsInReverseOrderLazy = new(
                                () => IncrementStrategyFinder.GetMergedCommits(item, 0).Reverse().ToList()
                            );
                            childConfiguration = configuration;

                        }

                        var childIteration = CreateIteration(
                            branchName: mergeMessage.MergedBranch,
                            configuration: childConfiguration,
                            parent: iteration
                        );

                        var done = IterateOverCommitsRecursive(
                            commitsInReverseOrder: mergedCommitsInReverseOrderLazy.Value,
                            iteration: childIteration,
                            targetLabel: targetLabel,
                            traversedCommits: traversedCommits
                        );

                        commit.AddChildIteration(childIteration);
                        if (done) return true;
                    }

                    traversedCommits.AddRange(mergedCommitsInReverseOrderLazy.Value);
                }
            }
        }
        return false;
    }

    private VersionField GetIncrementForcedByCommit(ICommit commit, EffectiveConfiguration configuration)
    {
        commit.NotNull();
        configuration.NotNull();

        return configuration.CommitMessageIncrementing switch
        {
            CommitMessageIncrementMode.Enabled => IncrementStrategyFinder.GetIncrementForcedByCommit(commit, configuration),
            CommitMessageIncrementMode.Disabled => VersionField.None,
            CommitMessageIncrementMode.MergeMessageOnly => commit.IsMergeCommit
                ? IncrementStrategyFinder.GetIncrementForcedByCommit(commit, configuration) : VersionField.None,
            _ => throw new InvalidEnumArgumentException(
                nameof(configuration.CommitMessageIncrementing), (int)configuration.CommitMessageIncrementing, typeof(CommitMessageIncrementMode)
            )
        };
    }

    private IReadOnlyDictionary<ICommit, EffectiveBranchConfiguration> GetCommitsWasBranchedFrom(ReferenceName branchName)
    {
        Dictionary<ICommit, EffectiveBranchConfiguration> result = new();

        var branch = RepositoryStore.FindBranch(branchName);
        if (branch is null) return result;

        var branchCommits = RepositoryStore.FindCommitBranchesWasBranchedFrom(
            branch, Context.Configuration
        ).ToList();

        var branchCommitDictionary = branchCommits.ToDictionary(
            element => element.Branch, element => element.Commit
        );
        foreach (var item in branchCommitDictionary.Keys)
        {
            var branchConfiguration = Context.Configuration.GetBranchConfiguration(item);
            if (branchConfiguration.Increment == IncrementStrategy.Inherit) continue;

            if (result.ContainsKey(branchCommitDictionary[item]))
            {
                if ((branchConfiguration.IsMainline ?? Context.Configuration.IsMainline) == true
                    && !result[branchCommitDictionary[item]].Value.IsMainline)
                {
                    result[branchCommitDictionary[item]]
                        = new(new EffectiveConfiguration(Context.Configuration, branchConfiguration), item);
                }
            }
            else
            {
                result.Add(
                    key: branchCommitDictionary[item],
                    value: new(new EffectiveConfiguration(Context.Configuration, branchConfiguration), item)
                );
            }
        }
        return result;
    }

    private static BaseVersionV2 DetermineBaseVersion(
        TrunkBasedIteration iteration, string? targetLabel,
        IReadOnlyDictionary<string, HashSet<SemanticVersion>> taggedSemanticVersions
    ) => DetermineBaseVersionRecursive(iteration, targetLabel, taggedSemanticVersions);

    internal static BaseVersionV2 DetermineBaseVersionRecursive(
        TrunkBasedIteration iteration, string? targetLabel,
        IReadOnlyDictionary<string, HashSet<SemanticVersion>> taggedSemanticVersions)
    {
        iteration.NotNull();
        taggedSemanticVersions.NotNull();

        var incrementSteps = GetIncrementSteps(iteration, targetLabel, taggedSemanticVersions).ToArray();

        var semanticVersion = SemanticVersion.Empty;

        for (var i = 0; i < incrementSteps.Length; i++)
        {
            var incrementStep = incrementSteps[i];
            if (incrementStep.SemanticVersion is not null)
                semanticVersion = incrementStep.SemanticVersion;

            if (i + 1 < incrementSteps.Length)
            {
                if (incrementStep.ShouldIncrement)
                {
                    semanticVersion = semanticVersion.Increment(
                        incrementStep.Increment, incrementStep.Label, incrementStep.ForceIncrement
                    );
                    if (semanticVersion.IsLessThan(incrementStep.AlternativeSemanticVersion, includePreRelease: false))
                    {
                        semanticVersion = new SemanticVersion(semanticVersion)
                        {
                            Major = incrementStep.AlternativeSemanticVersion!.Major,
                            Minor = incrementStep.AlternativeSemanticVersion.Minor,
                            Patch = incrementStep.AlternativeSemanticVersion.Patch
                        };
                    }
                }
            }
            else
            {
                return new BaseVersionV2(nameof(TrunkBasedVersionStrategy), incrementStep.ShouldIncrement, semanticVersion, incrementStep.BaseVersionSource, null)
                {
                    Increment = incrementStep.Increment,
                    Label = incrementStep.Label,
                    ForceIncrement = incrementStep.ForceIncrement,
                    AlternativeSemanticVersion = incrementStep.AlternativeSemanticVersion
                };
            }
        }

        throw new InvalidOperationException();
    }

    private static IEnumerable<BaseVersionV2> GetIncrementSteps(TrunkBasedIteration iteration,
        string? targetLabel, IReadOnlyDictionary<string, HashSet<SemanticVersion>> taggedSemanticVersions)
    {
        TrunkBasedContext context = new(taggedSemanticVersions)
        {
            TargetLabel = targetLabel,
        };

        foreach (var commit in iteration.Commits)
        {
            foreach (var item in TrunkContextPreEnricherCollection)
            {
                item.Enrich(iteration, commit, context);
            }

            foreach (var incrementer in TrunkIncrementerCollection
                .Where(element => element.MatchPrecondition(iteration, commit, context)))
            {
                foreach (var item in incrementer.GetIncrements(iteration, commit, context))
                {
                    yield return item;
                }
            }

            foreach (var item in TrunkContextPostEnricherCollection)
            {
                item.Enrich(iteration, commit, context);
            }
        }
    }

    private static readonly IReadOnlyCollection<ITrunkBasedContextPreEnricher> TrunkContextPreEnricherCollection = new ITrunkBasedContextPreEnricher[]
    {
        new EnrichSemanticVersion(),
        new EnrichIncrement()
    };
    private static readonly IReadOnlyCollection<ITrunkBasedContextPostEnricher> TrunkContextPostEnricherCollection = new ITrunkBasedContextPostEnricher[]
    {
        new RemoveSemanticVersion(),
        new RemoveIncrement()
    };
    private static readonly IReadOnlyCollection<ITrunkBasedIncrementer> TrunkIncrementerCollection = new ITrunkBasedIncrementer[]
    {
        // Trunk
        new CommitOnTrunk(),

        new CommitOnTrunkWithPreReleaseTag(),
        new LastCommitOnTrunkWithPreReleaseTag(),

        new CommitOnTrunkWithStableTag(),
        new LastCommitOnTrunkWithStableTag(),

        new MergeCommitOnTrunk(),
        new LastMergeCommitOnTrunk(),

        new CommitOnTrunkBranchedToTrunk(),
        new CommitOnTrunkBranchedToNonTrunk(),

        // NonTrunk
        new CommitOnNonTrunk(),
        new CommitOnNonTrunkWithPreReleaseTag(),
        new LastCommitOnNonTrunkWithPreReleaseTag(),

        new CommitOnNonTrunkWithStableTag(),
        new LastCommitOnNonTrunkWithStableTag(),

        new MergeCommitOnNonTrunk(),
        new LastMergeCommitOnNonTrunk(),

        new CommitOnNonTrunkBranchedToTrunk(),
        new CommitOnNonTrunkBranchedToNonTrunk()
    };
}
