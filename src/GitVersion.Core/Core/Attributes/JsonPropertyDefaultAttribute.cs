using GitVersion.Extensions;

namespace GitVersion.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class JsonPropertyDefaultAttribute : JsonAttribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="JsonPropertyDefaultAttribute"/> with the specified default property value.
    /// </summary>
    /// <param name="value">The default value of the property.</param>
    public JsonPropertyDefaultAttribute(string? value) => Value = value ?? "null";

    /// <inheritdoc cref="JsonPropertyDefaultAttribute(string)"/>
    public JsonPropertyDefaultAttribute(bool value) : this(value ? "true" : "false") { }

    /// <inheritdoc cref="JsonPropertyDefaultAttribute(string)"/>
    /// <remarks>The Enum value is converted to string, preferring the symbol name over the numeral if possible.</remarks>
    public JsonPropertyDefaultAttribute(SemanticVersionFormat value) : this(value.ToString()) { }
    /// <inheritdoc cref="JsonPropertyDefaultAttribute(SemanticVersionFormat)"/>
    public JsonPropertyDefaultAttribute(AssemblyVersioningScheme value) : this(value.ToString()) { }
    /// <inheritdoc cref="JsonPropertyDefaultAttribute(SemanticVersionFormat)"/>
    public JsonPropertyDefaultAttribute(AssemblyFileVersioningScheme value) : this(value.ToString()) { }

    /// <inheritdoc cref="JsonPropertyDefaultAttribute(string)"/>
    /// <remarks>Depending on the Type of the boxed value, the resulting string will be automatically enclosed with single-quotes. If the boxed value is NOT a string and is converted to a string from a numeric , object, array, boolean, or null-only type, then it will NOT be enclosed with single-quotes.</remarks>
    public JsonPropertyDefaultAttribute(object boxedValue)
    {
        if (boxedValue is not null)
        {
            var type = boxedValue.GetType();
            var typedObj = Convert.ChangeType(boxedValue, type);
            Value = typedObj.ToString() ?? string.Empty;
            if (Value == type.ToString())
                Value = JsonSerializer.Serialize(typedObj, type);
        }
        else
        {
            Value = "null";
        }
    }

    /// <summary>
    /// The description of the property.
    /// </summary>
    public string Value { get; }
}
