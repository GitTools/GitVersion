namespace GitVersion.VersionCalculation;

internal class BaseVersionV2 : BaseVersion
{
    internal static BaseVersionV2 ShouldIncrementFalse(string source, ICommit? baseVersionSource, string? label, SemanticVersion? alternativeSemanticVersion = null) => new(source, false)
    {
        BaseVersionSource = baseVersionSource,
        Increment = VersionField.None,
        SemanticVersion = null,
        Label = label,
        AlternativeSemanticVersion = alternativeSemanticVersion
    };

    internal static BaseVersionV2 ShouldIncrementFalse(string source, ICommit? baseVersionSource, SemanticVersion semanticVersion) => new(source, false)
    {
        BaseVersionSource = baseVersionSource,
        Increment = VersionField.None,
        SemanticVersion = semanticVersion
    };

    internal static BaseVersionV2 ShouldIncrementTrue(string source, ICommit? baseVersionSource, VersionField increment, string? label, bool forceIncrement, SemanticVersion? alternativeSemanticVersion = null) => new(source, true)
    {
        BaseVersionSource = baseVersionSource,
        Increment = increment,
        Label = label,
        ForceIncrement = forceIncrement,
        AlternativeSemanticVersion = alternativeSemanticVersion
    };

    public BaseVersionV2(string source, bool shouldIncrement) : base(source, shouldIncrement)
    {
    }

    public BaseVersionV2(string source, bool shouldIncrement, SemanticVersion semanticVersion, ICommit? baseVersionSource, string? branchNameOverride)
        : base(source, shouldIncrement, semanticVersion, baseVersionSource, branchNameOverride)
    {
    }

    public bool ForceIncrement { get; init; }

    public VersionField Increment { get; init; }

    public string? Label { get; init; }

    internal SemanticVersion? AlternativeSemanticVersion { get; init; }

    public override string ToString()
    {
        var commitSource = BaseVersionSource?.Id.ToString(7) ?? "External";

        StringBuilder stringBuilder = new();
        if (ShouldIncrement)
        {
            stringBuilder.Append($"{Source}: ");
            if (ForceIncrement)
                stringBuilder.Append("Force version increment ");
            else
            {
                stringBuilder.Append("Version increment ");
            }

            if (SemanticVersion is not null)
                stringBuilder.Append($"'{SemanticVersion:f}' ");

            stringBuilder.Append($"+semver '{Increment}'");

            if (Label is null)
                stringBuilder.Append(" with no label");
            else
            {
                stringBuilder.Append($" with label '{Label}'");
            }
        }
        else if (SemanticVersion is null)
        {
            stringBuilder.Append($"{Source}: Label as '{Label}'");
        }
        else
        {
            stringBuilder.Append($"{Source}: Take '{GetSemanticVersion():f}'");
        }

        if (BaseVersionSource is not null)
            stringBuilder.Append($" based on commit '{commitSource}'.");
        return stringBuilder.ToString();
    }
}
