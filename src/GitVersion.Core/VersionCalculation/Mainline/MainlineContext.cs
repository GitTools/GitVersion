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

    public string? Label { get; set; }

    public VersionField Increment { get; set; }

    public bool SuppressBranchIncrement { get; set; }

    public ICommit? BaseVersionSource { get; set; }

    public HashSet<SemanticVersion> AlternativeSemanticVersions { get; } = [];

    public bool ForceIncrement { get; set; }
}
