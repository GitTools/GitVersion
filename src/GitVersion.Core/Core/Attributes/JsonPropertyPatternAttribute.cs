namespace GitVersion.Attributes;

/// <summary>
/// The <c>pattern</c> keyword is used to restrict a <see langword="string"/> property to a particular regular expression.<br/>
/// <see href="https://json-schema.org/understanding-json-schema/reference/string#regexp"/>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class JsonPropertyPatternAttribute : JsonAttribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="JsonPropertyPatternAttribute"/> with the specified property pattern.
    /// </summary>
    /// <param name="pattern">The pattern of the property.</param>>
    public JsonPropertyPatternAttribute(string pattern) => Pattern = pattern;

    /// <summary>
    /// The Regular Expression pattern of the property.
    /// </summary>
    public string Pattern { get; }
}
