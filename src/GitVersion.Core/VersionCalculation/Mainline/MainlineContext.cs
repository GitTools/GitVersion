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

    public SemanticVersion? SemanticVersion { get; set; }

    public SemanticVersionSource? SemVerSource { get; set; }

    public string? Label { get; set; }

    public VersionField Increment { get; set; }

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
