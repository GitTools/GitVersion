using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation.Mainline;

internal record MainlineContext(MainlineVersionCalculator Calculator)
{
    internal MainlineVersionCalculator Calculator { get; } = Calculator.NotNull();

    public IIncrementStrategyFinder IncrementStrategyFinder => Calculator.IncrementStrategyFinder;

    public IGitVersionConfiguration Configuration => Calculator.History.Configuration;

    public IEnvironment Environment => Calculator.History.Environment;

    // Keep the calculation commit unchanged while traversing ancestors and merged branches.
    public ICommit CurrentCommit => Calculator.History.CurrentCommit;

    public string? TargetLabel { get; init; }

    public IMainlineCommitResolver CommitResolver => Calculator.CommitResolver;

    internal MainlineConfigurationResolver ConfigurationResolver => Calculator.ConfigurationResolver;

    public EffectiveConfiguration GetEffectiveConfiguration(MainlineIteration iteration)
        => ConfigurationResolver.GetEffectiveConfiguration(iteration);

    public EffectiveConfiguration GetEffectiveConfiguration(MainlineCommit commit)
        => ConfigurationResolver.GetEffectiveConfiguration(commit);

    public bool IsMergedMainline(MainlineCommit commit)
        => ConfigurationResolver.IsMergedMainline(commit);

    public VersionField GetIncrementForcedByBranch(MainlineCommit commit)
        => ConfigurationResolver.GetIncrementForcedByBranch(commit);

    // Independent source calculations also need side histories stored in other iterations.
    public bool IncludeSharedHistory { get; init; }

    public bool HasChildHistory(MainlineCommit commit)
    {
        if (commit.HasChildIteration && Configuration.Ignore.IsEmpty)
        {
            return true;
        }
        return commit.ChildIteration is { } child
            && (commit.HasChildIteration || IncludeSharedHistory && child.BranchedFrom is not null)
            && Calculator.History.Select(
                child, TargetLabel, false, includeSharedHistory: IncludeSharedHistory
            ).Commits.Length > 0;
    }

    public MainlineContext CreateChildContext()
        => new(Calculator)
        {
            TargetLabel = TargetLabel,
            IncludeSharedHistory = IncludeSharedHistory,
            MainlineVersions = MainlineVersions
        };

    public Dictionary<MainlineIteration, BaseVersion> MainlineVersions { get; init; } = [];

    internal MainlineIncrementState Increments { get; } = new();

    internal bool IsMainlineMerge { get; set; }

    public MainlineCommit? Predecessor { get; set; }

    public bool? HasSuccessor { get; set; }

    public MainlineCommit? GetPredecessor(MainlineCommit commit)
        => HasSuccessor.HasValue ? Predecessor : commit.Predecessor;

    public bool IsLastCommit(MainlineCommit commit) => !(HasSuccessor ?? commit.HasSuccessor);

    public SemanticVersion? SemanticVersion { get; set; }

    public SemanticVersionSource? SemVerSource { get; set; }

    public string? Label { get; set; }

    public VersionField Increment { get; set; }

    public VersionField CommitIncrement { get; set; }

    public bool SuppressBranchIncrement { get; set; }

    public ICommit? BaseVersionSource { get; set; }

    public HashSet<SemanticVersion> AlternativeSemanticVersions { get; } = [];

    private readonly Dictionary<SemanticVersion, SemanticVersionSource> alternativeSources = [];

    public void AddAlternativeSemanticVersion(SemanticVersion version, SemanticVersionSource source)
    {
        AlternativeSemanticVersions.Add(version);
        this.alternativeSources.TryAdd(version, source);
    }

    public SemanticVersionSource? GetAlternativeSemVerSource()
    {
        var version = AlternativeSemanticVersions.Max();
        return version is not null && this.alternativeSources.TryGetValue(version, out var source) ? source : null;
    }

    public void ClearAlternativeSemanticVersions()
    {
        AlternativeSemanticVersions.Clear();
        this.alternativeSources.Clear();
    }

    public bool ForceIncrement { get; set; }
}
