using GitVersion.Configuration;
using GitVersion.Git;

namespace GitVersion.VersionCalculation.Mainline;

// CurrentCommit stays fixed while traversing ancestors and merged branches.
internal record MainlineContext(ICommit CurrentCommit)
{
    public string? TargetLabel { get; init; }

    public EffectiveConfiguration EffectiveConfiguration { get; set; } = null!;

    public EffectiveConfiguration IterationConfiguration { get; set; } = null!;

    public EffectiveConfiguration? ChildConfiguration { get; set; }

    // Independent source calculations also need side histories stored in other iterations.
    public bool IncludeSharedHistory { get; init; }

    public MainlineContext CreateChildContext()
        => new(CurrentCommit)
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
