#if NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace GitVersion.Testing.Internal;

internal static class StringBuilderExtensions
{
    public static void AppendLineFormat(this StringBuilder stringBuilder,
#if NET7_0_OR_GREATER
                                        [StringSyntax(StringSyntaxAttribute.CompositeFormat)]
#endif
                                        string format,
                                        params object?[] args)
    {
        stringBuilder.AppendFormat(format, args);
        stringBuilder.AppendLine();
    }
}
