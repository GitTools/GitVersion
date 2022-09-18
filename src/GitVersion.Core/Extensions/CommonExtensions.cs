using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GitVersion.Extensions;

public static class CommonExtensions
{
    public static T NotNull<T>([NotNull] this T? value, [CallerArgumentExpression("value")] string name = "")
        where T : class => value ?? throw new ArgumentNullException(name);

    public static string NotNullOrEmpty([NotNull] this string? value, [CallerArgumentExpression("value")] string name = "")
        => string.IsNullOrEmpty(value) ? throw new ArgumentException("The parameter is null or empty.", name) : value!;

    public static string NotNullOrWhitespace([NotNull] this string? value, [CallerArgumentExpression("value")] string name = "")
        => string.IsNullOrWhiteSpace(value) ? throw new ArgumentNullException("The parameter is null or empty or contains only whitspaces.", name) : value!;
}
