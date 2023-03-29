namespace GitVersion.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class JsonPropertyDescriptionAttribute : JsonAttribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="JsonPropertyDescriptionAttribute"/> with the specified property description.
    /// </summary>
    /// <param name="description">The description of the property.</param>
    public JsonPropertyDescriptionAttribute(string description) => Description = description;

    /// <summary>
    /// The description of the property.
    /// </summary>
    public string Description { get; }
}
