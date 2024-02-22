namespace GitVersion;

public sealed record SemanticVersionWithTag(SemanticVersion Value, ITag Tag) : IComparable<SemanticVersionWithTag>
{
    public int CompareTo(SemanticVersionWithTag? other) => Value.CompareTo(other?.Value);

    public override string ToString() => $"{Tag} | {Tag.Commit} | {Value}";
}
