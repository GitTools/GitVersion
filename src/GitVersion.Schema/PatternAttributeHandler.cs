using GitVersion.Attributes;
using Json.Schema.Generation;
using Json.Schema.Generation.Intents;

namespace GitVersion.Schema;
internal class PatternAttributeHandler : IAttributeHandler<JsonPropertyPatternAttribute>
{
    void IAttributeHandler.AddConstraints(SchemaGenerationContextBase context, Attribute attribute)
    {
        if (attribute is JsonPropertyPatternAttribute patternAttribute)
        {
            context.Intents.Insert(0, new PatternIntent(patternAttribute.Pattern));
        }
    }
}
