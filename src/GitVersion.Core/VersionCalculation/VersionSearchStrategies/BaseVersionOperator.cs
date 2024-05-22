using GitVersion.Git;

namespace GitVersion.VersionCalculation;

public sealed record BaseVersionOperator : IBaseVersionIncrement
{
    public string Source { get; init; } = string.Empty;

    public ICommit? BaseVersionSource { get; init; }

    public VersionField Increment { get; init; }

    public bool ForceIncrement { get; init; }

    public string? Label { get; init; }

    public SemanticVersion? AlternativeSemanticVersion { get; init; }

    public override string ToString()
    {
        StringBuilder stringBuilder = new();

        stringBuilder.Append($"{Source}: ");
        if (ForceIncrement)
            stringBuilder.Append("Force version increment ");
        else
        {
            stringBuilder.Append("Version increment ");
        }

        stringBuilder.Append($"+semver '{Increment}'");

        if (Label is null)
            stringBuilder.Append(" with no label");
        else
        {
            stringBuilder.Append($" with label '{Label}'");
        }

        if (BaseVersionSource is not null)
            stringBuilder.Append($" based on commit '{BaseVersionSource.Id.ToString(7)}'.");

        return stringBuilder.ToString();
    }
}
