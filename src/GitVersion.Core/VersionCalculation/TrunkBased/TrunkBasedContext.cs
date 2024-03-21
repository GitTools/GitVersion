using GitVersion.Git;

namespace GitVersion.VersionCalculation.TrunkBased;

internal record TrunkBasedContext
{
    public string? TargetLabel { get; init; }

    public SemanticVersion? SemanticVersion { get; set; }

    public string? Label { get; set; }

    public VersionField Increment { get; set; }

    public ICommit? BaseVersionSource { get; set; }

    public HashSet<SemanticVersion> AlternativeSemanticVersions { get; } = [];

    public bool ForceIncrement { get; set; }
}
