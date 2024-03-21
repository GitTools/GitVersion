namespace GitVersion.VersionCalculation;

public sealed record class BaseVersionOperator : IBaseVersionIncrement
{
    public string Source { get; init; } = string.Empty;

    public ICommit? BaseVersionSource { get; init; }

    public VersionField Increment { get; init; }

    public bool ForceIncrement { get; init; }

    public string? Label { get; init; }

    public SemanticVersion? AlternativeSemanticVersion { get; init; }
}
