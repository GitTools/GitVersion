using System.Diagnostics.CodeAnalysis;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

public sealed record class BaseVersion : IBaseVersion
{
    public BaseVersion() : this(new BaseVersionOperand())
    {
    }

    public BaseVersion(string source, SemanticVersion semanticVersion, ICommit? baseVersionSource = null)
        : this(new BaseVersionOperand(source, semanticVersion, baseVersionSource))
    {
    }

    public BaseVersion(BaseVersionOperand baseVersionOperand) => Operand = baseVersionOperand.NotNull();

    public string Source => (Operator?.Source).IsNullOrEmpty() ? Operand.Source : Operator.Source;

    public SemanticVersion SemanticVersion => Operand.SemanticVersion;

    public ICommit? BaseVersionSource => Operator?.BaseVersionSource ?? Operand.BaseVersionSource;

    [MemberNotNullWhen(true, nameof(Operator))]
    public bool ShouldBeIncremented => Operator is not null;

    public BaseVersionOperand Operand { get; init; }

    public BaseVersionOperator? Operator { get; init; }

    public SemanticVersion GetIncrementedVersion()
    {
        var result = SemanticVersion;

        if (ShouldBeIncremented)
        {
            result = result.Increment(
                increment: Operator.Increment,
                label: Operator.Label,
                forceIncrement: Operator.ForceIncrement
            );

            if (result.IsLessThan(Operator.AlternativeSemanticVersion, includePreRelease: false))
            {
                result = new SemanticVersion(result)
                {
                    Major = Operator.AlternativeSemanticVersion!.Major,
                    Minor = Operator.AlternativeSemanticVersion.Minor,
                    Patch = Operator.AlternativeSemanticVersion.Patch
                };
            }
        }

        return result;
    }

    public override string ToString()
    {
        var commitSource = BaseVersionSource?.Id.ToString(7) ?? "External";

        StringBuilder stringBuilder = new();
        if (ShouldBeIncremented)
        {
            stringBuilder.Append($"{Source}: ");
            if (Operator.ForceIncrement)
                stringBuilder.Append("Force version increment ");
            else
            {
                stringBuilder.Append("Version increment ");
            }

            if (SemanticVersion is not null)
                stringBuilder.Append($"'{SemanticVersion:f}' ");

            stringBuilder.Append($"+semver '{Operator.Increment}'");

            if (Operator.Label is null)
                stringBuilder.Append(" with no label");
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
