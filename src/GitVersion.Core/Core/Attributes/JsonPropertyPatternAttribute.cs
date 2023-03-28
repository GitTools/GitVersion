namespace GitVersion.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class JsonPropertyPatternAttribute : JsonAttribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="JsonPropertyPatternAttribute"/> with the specified property pattern.
    /// </summary>
    /// <param name="pattern">The pattern of the property.</param>
    /// <param name="format">The pattern format</param>
    public JsonPropertyPatternAttribute(string pattern, PatternFormat format = PatternFormat.Regex)
    {
        Pattern = pattern;
        Format = format;
    }

    /// <summary>
    /// The pattern of the property.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// The format of the pattern.
    /// </summary>
    public PatternFormat Format { get; }
}

public enum PatternFormat
{
    /// <summary>
    /// The pattern is a regular expression.
    /// </summary>
    Regex,

    /// <summary>
    /// The pattern is a datetime pattern.
    /// </summary>
    DateTime
}
