using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

public sealed record class BaseVersionOperand : IBaseVersionIncrement
{
    public BaseVersionOperand() : this(string.Empty, SemanticVersion.Empty)
    {
    }

    public BaseVersionOperand(string source, SemanticVersion semanticVersion, ICommit? baseVersionSource = null)
    {
        Source = source.NotNull();
        SemanticVersion = semanticVersion.NotNull();
        BaseVersionSource = baseVersionSource;
    }

    public string Source { get; init; }

    public SemanticVersion SemanticVersion { get; init; }

    public ICommit? BaseVersionSource { get; init; }
}
