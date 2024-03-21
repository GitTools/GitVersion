using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

public sealed record BaseVersionOperand(string Source, SemanticVersion SemanticVersion, ICommit? BaseVersionSource = null)
    : IBaseVersionIncrement
{
    public BaseVersionOperand() : this(string.Empty, SemanticVersion.Empty)
    {
    }

    public string Source { get; init; } = Source.NotNull();

    public SemanticVersion SemanticVersion { get; init; } = SemanticVersion.NotNull();
}
