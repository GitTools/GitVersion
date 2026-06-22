namespace GitVersion.Formatting;

internal class LabelToken
{
    public LabelToken(string name, LabelTokenType type, string? format = null)
    {
        Name = name;
        Type = type;
        Format = format;
    }

    public string Name { get; }

    public LabelTokenType Type { get; }

    public string? Format { get; }
}
