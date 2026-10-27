using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation.Mainline;

internal record MainlineContext(IIncrementStrategyFinder IncrementStrategyFinder, IGitVersionConfiguration Configuration, IEnvironment Environment, ICommit CurrentCommit)
{
    public IIncrementStrategyFinder IncrementStrategyFinder { get; } = IncrementStrategyFinder.NotNull();

    public IGitVersionConfiguration Configuration { get; } = Configuration.NotNull();

    public IEnvironment Environment { get; } = Environment.NotNull();

    // Keep the calculation commit unchanged while traversing ancestors and merged branches.
    public ICommit CurrentCommit { get; } = CurrentCommit.NotNull();

    public string? TargetLabel { get; init; }

    public MainlineBranchPointResolver? BranchPointResolver { get; set; }

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
            && new MainlineHistory(Configuration, Environment, CurrentCommit, BranchPointResolver)
                .Select(child, TargetLabel, false, includeSharedHistory: IncludeSharedHistory).Commits.Length > 0;
    }

    public MainlineContext CreateChildContext()
        => new(IncrementStrategyFinder, Configuration, Environment, CurrentCommit)
        {
            TargetLabel = TargetLabel,
            BranchPointResolver = BranchPointResolver,
            IncludeSharedHistory = IncludeSharedHistory,
            MainlineVersions = MainlineVersions
        };

    public Dictionary<MainlineIteration, BaseVersion> MainlineVersions { get; init; } = [];

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
