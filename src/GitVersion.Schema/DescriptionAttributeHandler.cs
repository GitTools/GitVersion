using GitVersion.Attributes;
using Json.Schema.Generation;
using Json.Schema.Generation.Intents;

namespace GitVersion.Schema;

internal class DescriptionAttributeHandler : IAttributeHandler<JsonPropertyDescriptionAttribute>
{
    void IAttributeHandler.AddConstraints(SchemaGenerationContextBase context, Attribute attribute)
    {
        if (attribute is JsonPropertyDescriptionAttribute descriptionAttribute)
        {
            context.Intents.Insert(0, new DescriptionIntent(descriptionAttribute.Description));
        }
    }
}
