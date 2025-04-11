using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Intents;
using Format = GitVersion.Configuration.Attributes.Format;
using FormatAttribute = GitVersion.Configuration.Attributes.JsonPropertyFormatAttribute;

namespace GitVersion.Schema;
internal class FormatAttributeHandler : IAttributeHandler<FormatAttribute>
{
    void IAttributeHandler.AddConstraints(SchemaGenerationContextBase context, Attribute attribute)
    {
        if (attribute is FormatAttribute formatAttribute)
        {
            var format = formatAttribute.Format switch
            {
                Format.Date => Formats.Date,
                Format.DateTime => Formats.DateTime,
                Format.Duration => Formats.Duration,
                Format.Email => Formats.Email,
                Format.Hostname => Formats.Hostname,
                Format.IdnEmail => Formats.IdnEmail,
                Format.IdnHostname => Formats.IdnHostname,
                Format.Ipv4 => Formats.Ipv4,
                Format.Ipv6 => Formats.Ipv6,
                Format.Iri => Formats.Iri,
                Format.IriReference => Formats.IriReference,
                Format.JsonPointer => Formats.JsonPointer,
                Format.Regex => Formats.Regex,
                Format.RelativeJsonPointer => Formats.RelativeJsonPointer,
                Format.Time => Formats.Time,
                Format.Uri => Formats.Uri,
                Format.UriReference => Formats.UriReference,
                Format.UriTemplate => Formats.UriTemplate,
                Format.Uuid => Formats.Uuid,
                _ => null // in case new formats are added.
            };

            if (format != null)
                context.Intents.Insert(0, new FormatIntent(format));
        }
    }
}
