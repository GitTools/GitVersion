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

    public override string ToString()
    {
        StringBuilder stringBuilder = new();

        stringBuilder.Append($"{Source}: Take '{SemanticVersion:f}'");

        if (BaseVersionSource is not null)
            stringBuilder.Append($" based on commit '{BaseVersionSource.Id.ToString(7)}'.");

        return stringBuilder.ToString();
    }
}
