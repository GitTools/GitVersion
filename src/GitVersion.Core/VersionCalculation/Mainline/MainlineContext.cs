using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation.Mainline;

internal record MainlineContext(IIncrementStrategyFinder IncrementStrategyFinder, IGitVersionConfiguration Configuration)
{
    public IIncrementStrategyFinder IncrementStrategyFinder { get; } = IncrementStrategyFinder.NotNull();

    public IGitVersionConfiguration Configuration { get; } = Configuration.NotNull();

    public string? TargetLabel { get; init; }

    public SemanticVersion? SemanticVersion { get; set; }

    public string? Label { get; set; }

    public VersionField Increment { get; set; }

    public ICommit? BaseVersionSource { get; set; }

    public HashSet<SemanticVersion> AlternativeSemanticVersions { get; } = [];

    public bool ForceIncrement { get; set; }
}
