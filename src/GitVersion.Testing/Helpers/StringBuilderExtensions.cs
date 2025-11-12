using System.Diagnostics.CodeAnalysis;

namespace GitVersion.Testing.Internal;

internal static class StringBuilderExtensions
{
    extension(StringBuilder stringBuilder)
    {
        public void AppendLineFormat([StringSyntax(StringSyntaxAttribute.CompositeFormat)]
                                     string format,
                                     params object?[] args)
        {
            stringBuilder.AppendFormat(format, args);
            stringBuilder.AppendLine();
        }
    }
}
