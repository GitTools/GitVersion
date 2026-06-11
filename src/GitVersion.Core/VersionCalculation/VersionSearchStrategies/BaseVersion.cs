using System.Diagnostics.CodeAnalysis;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

/// <summary>Represents a resolved base version consisting of an operand (the discovered version) and an optional operator (the increment to apply).</summary>
public sealed record BaseVersion(BaseVersionOperand Operand) : IBaseVersion
{
    /// <summary>Initializes an empty base version.</summary>
    public BaseVersion() : this(new BaseVersionOperand())
    {
    }

    /// <summary>Initializes a base version from a source description, semantic version, and optional source commit.</summary>
    public BaseVersion(string source, SemanticVersion semanticVersion, ICommit? baseVersionSource = null)
        : this(new BaseVersionOperand(source, semanticVersion, baseVersionSource))
    {
    }

    /// <summary>Gets the human-readable description of the source that produced this base version.</summary>
    public string Source => (Operator?.Source).IsNullOrEmpty() ? Operand.Source : Operator.Source;

    /// <summary>Gets the base semantic version before any increment is applied.</summary>
    public SemanticVersion SemanticVersion => Operand.SemanticVersion;

    /// <summary>Gets the version field that will be incremented, or <see cref="VersionField.None"/> when no increment is needed.</summary>
    public VersionField Increment => Operator?.Increment ?? VersionField.None;

    /// <summary>Gets the commit that is the source of this base version.</summary>
    public ICommit? BaseVersionSource => Operator?.BaseVersionSource ?? Operand.BaseVersionSource;

    /// <summary>Gets a value indicating whether this base version has a pending increment operator.</summary>
    [MemberNotNullWhen(true, nameof(Operator))]
    public bool ShouldIncrement => Operator is not null;

    /// <summary>Gets or initializes the operand that holds the discovered version.</summary>
    public BaseVersionOperand Operand { get; init; } = Operand.NotNull();

    /// <summary>Gets or initializes the optional operator that describes the increment to apply.</summary>
    public BaseVersionOperator? Operator { get; init; }

    /// <summary>Returns the semantic version after applying the operator increment, or the base version when no increment is needed.</summary>
    public SemanticVersion GetIncrementedVersion()
    {
        var result = SemanticVersion;

        if (ShouldIncrement)
        {
            result = result.Increment(
                increment: Operator.Increment,
                label: Operator.Label,
                forceIncrement: Operator.ForceIncrement,
                Operator.AlternativeSemanticVersion
            );
        }

        return result;
    }

    /// <summary>Returns a human-readable description of this base version including source, version, increment, and commit anchor.</summary>
    public override string ToString()
    {
        var commitSource = BaseVersionSource?.Id.ToString(7) ?? "External";

        StringBuilder stringBuilder = new();
        if (ShouldIncrement)
        {
            stringBuilder.Append($"{Source}: ");
            stringBuilder.Append(Operator.ForceIncrement ? "Force version increment " : "Version increment ");

            stringBuilder.Append($"'{SemanticVersion:f}' ");
            stringBuilder.Append($"+semver '{Operator.Increment}'");

            if (Operator.Label is null)
            {
                stringBuilder.Append(" with no label");
            }
            else
            {
                stringBuilder.Append($" with label '{Operator.Label}'");
            }
        }
        else
        {
            stringBuilder.Append($"{Source}: Take '{SemanticVersion:f}'");
        }

        if (BaseVersionSource is not null)
            stringBuilder.Append($" based on commit '{commitSource}'.");
        return stringBuilder.ToString();
    }

    internal BaseVersion Apply(BaseVersionOperator baseVersionOperator)
    {
        baseVersionOperator.NotNull();

        return new BaseVersion(Source, GetIncrementedVersion(), BaseVersionSource)
        {
            Operator = baseVersionOperator
        };
    }
}
