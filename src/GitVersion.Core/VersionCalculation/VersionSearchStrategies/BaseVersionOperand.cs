using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

/// <summary>Represents the discovered base version — the semantic version value and the commit it was found at.</summary>
public sealed record BaseVersionOperand(string Source, SemanticVersion SemanticVersion, ICommit? BaseVersionSource = null)
    : IBaseVersionIncrement
{
    /// <summary>Initializes an empty operand.</summary>
    public BaseVersionOperand() : this(string.Empty, SemanticVersion.Empty)
    {
    }

    /// <summary>Gets or initializes the human-readable description of the source that produced this operand.</summary>
    public string Source { get; init; } = Source.NotNull();

    /// <summary>Gets or initializes the discovered semantic version.</summary>
    public SemanticVersion SemanticVersion { get; init; } = SemanticVersion.NotNull();

    /// <summary>Returns a human-readable description of this operand including its source and version.</summary>
    public override string ToString()
    {
        StringBuilder stringBuilder = new();

        stringBuilder.Append($"{Source}: Take '{SemanticVersion:f}'");

        if (BaseVersionSource is not null)
            stringBuilder.Append($" based on commit '{BaseVersionSource.Id.ToString(7)}'.");

        return stringBuilder.ToString();
    }
}
