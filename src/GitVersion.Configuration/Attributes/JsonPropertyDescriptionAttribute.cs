#if GITVERSION_CONFIGURATION
namespace GitVersion.Configuration.Attributes;
#elif GITVERSION_OUTPUT
namespace GitVersion.Output.Attributes;
#endif

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class JsonPropertyDescriptionAttribute(string description) : JsonAttribute
{
    /// <summary>
    /// The description of the property.
    /// </summary>
    public string Description { get; } = description;
}
