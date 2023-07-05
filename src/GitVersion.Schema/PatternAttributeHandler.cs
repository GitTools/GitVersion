using GitVersion.Attributes;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Intents;

namespace GitVersion.Schema;
internal class PatternAttributeHandler : IAttributeHandler<JsonPropertyPatternAttribute>
{
    void IAttributeHandler.AddConstraints(SchemaGenerationContextBase context, Attribute attribute)
    {
        if (attribute is JsonPropertyPatternAttribute patternAttribute)
        {
            var format = patternAttribute.Format switch
            {
                PatternFormat.Regex => Formats.Regex,
                PatternFormat.DateTime => Formats.DateTime,
                _ => Formats.Regex
            };

            context.Intents.Insert(0, new PatternIntent(patternAttribute.Pattern));
            context.Intents.Insert(0, new FormatIntent(format));
        }
    }
}
