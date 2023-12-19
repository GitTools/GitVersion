using System.ComponentModel;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.VersionCalculation.TrunkBased;
using GitVersion.VersionCalculation.TrunkBased.NonTrunk;
using GitVersion.VersionCalculation.TrunkBased.Trunk;

namespace GitVersion.VersionCalculation;

internal sealed class TrunkBasedVersionStrategy : VersionStrategyBase
{
    private volatile int IterationCounter;

    private ITaggedSemanticVersionRepository TaggedSemanticVersionRepository { get; }

    private IRepositoryStore RepositoryStore { get; }

    private IIncrementStrategyFinder IncrementStrategyFinder { get; }

    public TrunkBasedVersionStrategy(Lazy<GitVersionContext> context, IRepositoryStore repositoryStore,
        ITaggedSemanticVersionRepository taggedSemanticVersionRepository, IIncrementStrategyFinder incrementStrategyFinder
    ) : base(context)
    {
        RepositoryStore = repositoryStore.NotNull();
        TaggedSemanticVersionRepository = taggedSemanticVersionRepository.NotNull();
        IncrementStrategyFinder = incrementStrategyFinder.NotNull();
    }

    public override IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
    {
        if (Context.Configuration.VersioningMode != VersioningMode.TrunkBased) yield break;

        var iteration = CreateIteration(branchName: Context.CurrentBranch.Name, configuration: configuration.Value);

        var taggedSemanticVersions = TaggedSemanticVersionRepository.GetTaggedSemanticVersions(
            Context.CurrentBranch, configuration.Value
        );

        var targetLabel = configuration.Value.GetBranchSpecificLabel(Context.CurrentBranch.Name, null);
        IterateOverCommitsRecursive(
            commitsInReverseOrder: Context.CurrentBranch.Commits,
            iteration: iteration,
            targetLabel: targetLabel,
            taggedSemanticVersions: taggedSemanticVersions
        );

        yield return DetermineBaseVersion(iteration, targetLabel);
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

    private bool IterateOverCommitsRecursive(
        IEnumerable<ICommit> commitsInReverseOrder, TrunkBasedIteration iteration, string? targetLabel,
        ILookup<ICommit, SemanticVersionWithTag> taggedSemanticVersions, HashSet<ICommit>? traversedCommits = null)
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
                taggedSemanticVersions = TaggedSemanticVersionRepository.GetTaggedSemanticVersions(
                    effectiveConfigurationWasBranchedFrom.Branch, effectiveConfigurationWasBranchedFrom.Value
                );
            }

            var incrementForcedByCommit = GetIncrementForcedByCommit(item, configuration);
            var commit = iteration.CreateCommit(item, branchName, configuration, incrementForcedByCommit);

            var semanticVersions = taggedSemanticVersions[item].ToArray();
            commit.AddSemanticVersions(semanticVersions.Select(element => element.Value));

            var label = targetLabel ?? configuration.GetBranchSpecificLabel(branchName, null);
            foreach (var semanticVersion in semanticVersions)
            {
                if (semanticVersion.Value.IsMatchForBranchSpecificLabel(label)) return true;
            }

            if (item.IsMergeCommit)
            {
                Lazy<IReadOnlyCollection<ICommit>> mergedCommitsInReverseOrderLazy = new(
                    () => IncrementStrategyFinder.GetMergedCommits(item, 1).Reverse().ToList()
                );

                if (configuration.TrackMergeMessage
                    && MergeMessage.TryParse(item, Context.Configuration, out var mergeMessage))
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
                            traversedCommits: traversedCommits,
                            taggedSemanticVersions: taggedSemanticVersions
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

    private static BaseVersionV2 DetermineBaseVersion(TrunkBasedIteration iteration, string? targetLabel)
        => DetermineBaseVersionRecursive(iteration, targetLabel);

    internal static BaseVersionV2 DetermineBaseVersionRecursive(TrunkBasedIteration iteration, string? targetLabel)
    {
        iteration.NotNull();

        var incrementSteps = GetIncrementSteps(iteration, targetLabel).ToArray();

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

    private static IEnumerable<BaseVersionV2> GetIncrementSteps(TrunkBasedIteration iteration, string? targetLabel)
    {
        TrunkBasedContext context = new()
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
