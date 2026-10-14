#if GITVERSION_CONFIGURATION
namespace GitVersion.Configuration.Attributes;
#elif GITVERSION_OUTPUT
namespace GitVersion.Output.Attributes;
#endif

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class JsonPropertyDescriptionAttribute(string description) : JsonAttribute // NOSONAR: Each consuming project defines the symbol selecting its namespace above.
{
    /// <summary>
    /// The description of the property.
    /// </summary>
    public string Description { get; } = description;
}
