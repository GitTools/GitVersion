using System.Diagnostics.CodeAnalysis;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

public sealed record BaseVersion(BaseVersionOperand Operand) : IBaseVersion
{
    public BaseVersion() : this(new BaseVersionOperand())
    {
    }

    public BaseVersion(string source, SemanticVersion semanticVersion, ICommit? baseVersionSource = null)
        : this(new BaseVersionOperand(source, semanticVersion, baseVersionSource))
    {
    }

    public string Source => (Operator?.Source).IsNullOrEmpty() ? Operand.Source : Operator.Source;

    public SemanticVersion SemanticVersion => Operand.SemanticVersion;

    public ICommit? BaseVersionSource => Operator?.BaseVersionSource ?? Operand.BaseVersionSource;

    [MemberNotNullWhen(true, nameof(Operator))]
    public bool ShouldIncrement => Operator is not null;

    public BaseVersionOperand Operand { get; init; } = Operand.NotNull();

    public BaseVersionOperator? Operator { get; init; }

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

    public override string ToString()
    {
        var commitSource = BaseVersionSource?.Id.ToString(7) ?? "External";

        StringBuilder stringBuilder = new();
        if (ShouldIncrement)
        {
            stringBuilder.Append($"{Source}: ");
            stringBuilder.Append(Operator.ForceIncrement ? "Force version increment " : "Version increment ");

            if (SemanticVersion is not null)
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
