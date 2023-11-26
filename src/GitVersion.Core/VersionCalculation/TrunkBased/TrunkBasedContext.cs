using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.TrunkBased;

internal record class TrunkBasedContext
{
    public string? TargetLabel { get; init; }

    public IReadOnlyDictionary<string, HashSet<SemanticVersion>> TaggedSemanticVersions { get; }

    public SemanticVersion? SemanticVersion { get; set; }

    public string? Label { get; set; }

    public VersionField Increment { get; set; }

    public ICommit? BaseVersionSource { get; set; }

    public HashSet<SemanticVersion> AlternativeSemanticVersions { get; } = new();

    public bool ForceIncrement { get; set; }

    public TrunkBasedContext(IReadOnlyDictionary<string, HashSet<SemanticVersion>> taggedSemanticVersions)
        => TaggedSemanticVersions = taggedSemanticVersions.NotNull();
}
