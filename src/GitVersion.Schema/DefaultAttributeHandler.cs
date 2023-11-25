using GitVersion.Attributes;
using Json.Schema.Generation;
using Json.Schema.Generation.Intents;

namespace GitVersion.Schema;

internal class DefaultAttributeHandler : IAttributeHandler<JsonPropertyDefaultAttribute>
{
    void IAttributeHandler.AddConstraints(SchemaGenerationContextBase context, Attribute attribute)
    {
        if (attribute is JsonPropertyDefaultAttribute defaultAttribute)
        {
            context.Intents.Insert(0, new DefaultIntent(defaultAttribute.Value));
        }
    }
}
