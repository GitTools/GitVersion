using Json.Schema.Generation;
using Json.Schema.Generation.Intents;

using ConfigurationDescriptionAttribute = GitVersion.Configuration.Attributes.JsonPropertyDescriptionAttribute;
using OutputDescriptionAttribute = GitVersion.Output.Attributes.JsonPropertyDescriptionAttribute;

namespace GitVersion.Schema;

internal class DescriptionAttributeHandler1 : IAttributeHandler<ConfigurationDescriptionAttribute>
{
    void IAttributeHandler.AddConstraints(SchemaGenerationContextBase context, Attribute attribute)
    {
        if (attribute is ConfigurationDescriptionAttribute descriptionAttribute)
        {
            context.Intents.Insert(0, new DescriptionIntent(descriptionAttribute.Description));
        }
    }
}

internal class DescriptionAttributeHandler2 : IAttributeHandler<OutputDescriptionAttribute>
{
    void IAttributeHandler.AddConstraints(SchemaGenerationContextBase context, Attribute attribute)
    {
        if (attribute is OutputDescriptionAttribute descriptionAttribute)
        {
            context.Intents.Insert(0, new DescriptionIntent(descriptionAttribute.Description));
        }
    }
}
