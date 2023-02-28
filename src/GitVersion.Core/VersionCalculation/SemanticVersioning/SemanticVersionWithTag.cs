namespace GitVersion;

public sealed record SemanticVersionWithTag(SemanticVersion Value, ITag Tag)
{
    public override string ToString() => $"{Tag} | {Tag.Commit} | {Value}";
}
