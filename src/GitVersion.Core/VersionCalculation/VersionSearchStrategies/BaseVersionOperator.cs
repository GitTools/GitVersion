using GitVersion.Git;

namespace GitVersion.VersionCalculation;

/// <summary>Describes the version increment operation that should be applied to a <see cref="BaseVersionOperand"/> to produce the next version.</summary>
public sealed record BaseVersionOperator : IBaseVersionIncrement
{
    /// <summary>Gets or initializes the human-readable source description for this increment.</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>Gets or initializes the commit that is the source of this operator.</summary>
    public ICommit? BaseVersionSource { get; init; }

    /// <summary>Gets or initializes the version field to increment.</summary>
    public VersionField Increment { get; init; }

    /// <summary>Gets or initializes a value indicating whether the increment should be applied unconditionally.</summary>
    public bool ForceIncrement { get; init; }

    /// <summary>Gets or initializes the pre-release label to apply after incrementing.</summary>
    public string? Label { get; init; }

    /// <summary>Gets or initializes an alternative semantic version that may be used instead when it is greater than the incremented version.</summary>
    public SemanticVersion? AlternativeSemanticVersion { get; init; }

    /// <summary>Returns a human-readable description of this operator including source, increment field, label, and commit anchor.</summary>
    public override string ToString()
    {
        StringBuilder stringBuilder = new();

        stringBuilder.Append($"{Source}: ");
        stringBuilder.Append(ForceIncrement ? "Force version increment " : "Version increment ");

        stringBuilder.Append($"+semver '{Increment}'");

        if (Label is null)
        {
            stringBuilder.Append(" with no label");
        }
        else
        {
            stringBuilder.Append($" with label '{Label}'");
        }

        if (BaseVersionSource is not null)
        {
            stringBuilder.Append($" based on commit '{BaseVersionSource.Id.ToString(7)}'.");
        }

        return stringBuilder.ToString();
    }
}
