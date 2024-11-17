using System.Diagnostics.CodeAnalysis;

namespace GitVersion.Testing.Internal;

internal static class StringBuilderExtensions
{
    public static void AppendLineFormat(this StringBuilder stringBuilder,
                                        [StringSyntax(StringSyntaxAttribute.CompositeFormat)]
                                        string format,
                                        params object?[] args)
    {
        stringBuilder.AppendFormat(format, args);
        stringBuilder.AppendLine();
    }
}
