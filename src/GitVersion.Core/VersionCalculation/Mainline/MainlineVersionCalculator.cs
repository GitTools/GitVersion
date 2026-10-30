using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.VersionCalculation.Mainline.NonTrunk;
using GitVersion.VersionCalculation.Mainline.Trunk;

namespace GitVersion.VersionCalculation.Mainline;

internal sealed class MainlineVersionCalculator
{
    private readonly MainlineHistory history;

    private MainlineVersionCalculator(MainlineIteration iteration, MainlineContext context)
    {
        BranchPointResolver = context.BranchPointResolver ?? new MainlineBranchPointResolver(iteration);
        ConfigurationResolver = new MainlineConfigurationResolver(context.Configuration, BranchPointResolver);
        this.history = new MainlineHistory(context.Configuration, context.Environment, context.CurrentCommit, BranchPointResolver, ConfigurationResolver);
        context.BranchPointResolver = BranchPointResolver;
        context.Calculator = this;
    }

    internal MainlineBranchPointResolver BranchPointResolver { get; }

    internal MainlineConfigurationResolver ConfigurationResolver { get; }

    internal MainlineHistory History => this.history;

    internal static MainlineVersionCalculator GetOrCreate(MainlineIteration iteration, MainlineContext context)
        => context.Calculator ?? new MainlineVersionCalculator(iteration, context);

    private static readonly IReadOnlyCollection<IContextPreEnricher> TrunkContextPreEnricherCollection =
    [
        new EnrichSemanticVersion(),
        new EnrichIncrement()
    ];
    private static readonly IReadOnlyCollection<IContextPostEnricher> TrunkContextPostEnricherCollection =
    [
        new RemoveSemanticVersion(),
        new RemoveIncrement()
    ];
    private static readonly IReadOnlyCollection<IIncrementer> TrunkIncrementerCollection =
    [
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
        new FirstCommitOnRelease(),

        new CommitOnNonTrunk(),
        new CommitOnNonTrunkWithPreReleaseTag(),
        new LastCommitOnNonTrunkWithPreReleaseTag(),

        new CommitOnNonTrunkWithStableTag(),
        new LastCommitOnNonTrunkWithStableTag(),

        new MergeCommitOnNonTrunk(),
        new LastMergeCommitOnNonTrunk(),

        new CommitOnNonTrunkBranchedToTrunk(),
        new CommitOnNonTrunkBranchedToNonTrunk()
    ];

    internal static BaseVersion GetMergedMainlineVersion(MainlineIteration iteration, MainlineContext context)
        => GetOrCreate(iteration, context).CalculateMergedMainlineVersionCached(iteration, context);

    private BaseVersion CalculateMergedMainlineVersionCached(MainlineIteration iteration, MainlineContext context)
    {
        // Inherited configurations depend on their enclosing iteration.
        var canCache = iteration.Configuration.Increment != IncrementStrategy.Inherit;
        if (canCache && context.MainlineVersions.TryGetValue(iteration, out var cachedVersion))
        {
            return cachedVersion;
        }
        var version = CalculateMergedMainlineVersion(iteration, context);
        if (canCache)
        {
            context.MainlineVersions.Add(iteration, version);
        }
        return version;
    }

    private BaseVersion CalculateMergedMainlineVersion(MainlineIteration iteration, MainlineContext context)
    {
        // The source tip can be ignored; the last recorded commit may be an
        // earlier tag and must not be mistaken for a released source tip.
        var tip = iteration.BranchedTo.NotNull().Parents[1];
        var configuration = context.GetEffectiveConfiguration(iteration);
        var label = configuration.GetBranchSpecificLabel(iteration.BranchName, null, context.Environment, tip);
        var sourceContext = context.CreateChildContext() with
        {
            TargetLabel = label,
            IncludeSharedHistory = true
        };
        var version = ApplyIncrements([.. GetIncrements(iteration, sourceContext, includeAncestry: true)]);
        // A released source tip contributes its tag, not its next pending increment.
        if (version.Operand.BaseVersionSource?.Equals(tip) == true
            && configuration.PreventIncrementWhenCurrentCommitTagged
            && version.Operator is { } operation)
        {
            version = version with { Operator = operation with { Increment = VersionField.None } };
        }
        return version;
    }

    internal static BaseVersion DetermineBaseVersion(MainlineIteration iteration, string? targetLabel,
            IIncrementStrategyFinder incrementStrategyFinder, IGitVersionConfiguration configuration, IEnvironment environment, ICommit currentCommit)
        => DetermineBaseVersionRecursive(iteration, targetLabel, incrementStrategyFinder, configuration, environment, currentCommit);

    internal static BaseVersion DetermineBaseVersionRecursive(MainlineIteration iteration, string? targetLabel,
        IIncrementStrategyFinder incrementStrategyFinder, IGitVersionConfiguration configuration, IEnvironment environment, ICommit currentCommit,
        Dictionary<MainlineIteration, BaseVersion>? mainlineVersions = null)
    {
        MainlineContext context = new(incrementStrategyFinder, configuration, environment, currentCommit)
        {
            TargetLabel = targetLabel,
            MainlineVersions = mainlineVersions ?? []
        };
        return GetOrCreate(iteration, context).CalculateBaseVersion(iteration, context);
    }

    internal BaseVersion CalculateBaseVersion(MainlineIteration iteration, MainlineContext context)
    {
        iteration.NotNull();
        var incrementSteps = GetIncrements(iteration, context, includeAncestry: false).ToArray();
        return ApplyIncrements(incrementSteps);
    }

    private static BaseVersion ApplyIncrements(IEnumerable<IBaseVersionIncrement> incrementSteps)
    {
        BaseVersion? result = null;
        SemanticVersion? iterationBaseline = null;
        foreach (var baseVersionIncrement in incrementSteps)
        {
            switch (baseVersionIncrement)
            {
                case BaseVersionOperand baseVersionOperand:
                    result = new BaseVersion(baseVersionOperand);
                    iterationBaseline = baseVersionOperand.SemanticVersion;
                    break;
                case BaseVersionOperator baseVersionOperator:
                    result ??= new BaseVersion();
                    result = result.Apply(baseVersionOperator);
                    break;
                case BaseVersion baseVersion:
                    result = baseVersion;
                    iterationBaseline = baseVersion.SemanticVersion;
                    break;
                case MergedMainlineIncrement merged:
                    result = ApplyMergedMainlineIncrement(result ?? new BaseVersion(), merged, iterationBaseline);
                    break;
            }
        }
        return result ?? throw new InvalidOperationException();
    }

    private IEnumerable<IBaseVersionIncrement> GetIncrements(MainlineIteration iteration, MainlineContext context, bool includeAncestry)
    {
        var (commits, _) = GetCalculationCommits(iteration, context, includeAncestry);
        for (var index = 0; index < commits.Length; index++)
        {
            var commit = commits[index];
            context.Predecessor = index > 0 ? commits[index - 1] : null;
            context.HasSuccessor = index < commits.Length - 1;
            foreach (var item in TrunkContextPreEnricherCollection)
            {
                item.Enrich(iteration, commit, context);
            }

            var isMainlineMerge = context.SemanticVersion is null && context.IsMergedMainline(commit);

            ThrowIfUnsupportedMerge(commit, context);

            foreach (var incrementer in TrunkIncrementerCollection
                .Where(element => element.MatchPrecondition(iteration, commit, context)))
            {
                foreach (var item in incrementer.GetIncrements(iteration, commit, context))
                {
                    yield return WithMergedMainlineVersion(item, commit, isMainlineMerge, context);
                }
            }

            foreach (var item in TrunkContextPostEnricherCollection)
            {
                item.Enrich(commit, context);
            }
        }
    }

    private static void ThrowIfUnsupportedMerge(MainlineCommit commit, MainlineContext context)
    {
        if (commit.HasUnsupportedMerge
            && (!commit.HasExplicitIncrement || context.SemanticVersion is null))
        {
            throw new NotSupportedException("GitVersion does not support more than one merge source in a single commit yet");
        }
    }

    private IBaseVersionIncrement WithMergedMainlineVersion(IBaseVersionIncrement increment, MainlineCommit commit,
        bool isMainlineMerge, MainlineContext context)
        => increment is BaseVersionOperator operation && isMainlineMerge && context.SemanticVersion is null
            && commit.ChildIteration is { } child
            && !context.GetEffectiveConfiguration(child).PreventIncrementWhenBranchMerged
            ? new MergedMainlineIncrement(operation, CalculateMergedMainlineVersionCached(child, context))
            : increment;

    private (MainlineCommit[] Commits, bool HasBoundary) GetCalculationCommits(MainlineIteration iteration,
        MainlineContext context, bool includeAncestry)
    {
        var selection = this.history.Select(iteration, context.TargetLabel, includeAncestry,
            includeSharedHistory: context.IncludeSharedHistory);
        if (selection.MissingCommit is { } missing)
        {
            throw new InvalidOperationException($"The recorded mainline history does not contain branch point '{missing.Sha}'.");
        }
        return (selection.Commits, selection.HasBoundary);
    }

    private sealed record MergedMainlineIncrement(BaseVersionOperator Operation, BaseVersion Version) : IBaseVersionIncrement
    {
        public string Source => Operation.Source;
        public ICommit? BaseVersionSource => Operation.BaseVersionSource;
    }

    private static BaseVersion ApplyMergedMainlineIncrement(BaseVersion receiver, MergedMainlineIncrement merged,
        SemanticVersion? baseline)
    {
        var receiverVersion = receiver.GetIncrementedVersion();
        var continuesReleasedPrerelease = baseline is { IsPreRelease: true }
            && baseline.Major == receiverVersion.Major && baseline.Minor == receiverVersion.Minor
            && baseline.Patch == receiverVersion.Patch;
        // Restart a generated unnamed counting suffix for the new count anchor.
        // A released prerelease, including a numeric tag such as 2.0.0-5, continues
        // its own sequence while the numeric release version remains unchanged.
        if (receiverVersion.PreReleaseTag.Name.IsNullOrEmpty() && !continuesReleasedPrerelease)
        {
            receiverVersion = new SemanticVersion(receiverVersion) { PreReleaseTag = SemanticVersionPreReleaseTag.Empty };
        }
        return new BaseVersion(receiver.Source, receiverVersion, receiver.BaseVersionSource)
        {
            SemVerSource = receiver.GetSemVerSource()
        }.Apply(WithMainlineVersion(merged.Operation,
            merged.Version.GetIncrementedVersion(), merged.Version.GetSemVerSource()));
    }

    private static BaseVersionOperator WithMainlineVersion(BaseVersionOperator operation, SemanticVersion version,
        SemanticVersionSource? source)
        => operation.AlternativeSemanticVersion is { } alternative && !alternative.IsLessThan(version, includePreRelease: false)
            ? operation
            : operation with { AlternativeSemanticVersion = version, AlternativeSemVerSource = source };
}
