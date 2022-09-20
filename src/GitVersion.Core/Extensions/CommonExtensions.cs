using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GitVersion.Extensions;

public static class CommonExtensions
{
    public static T NotNull<T>([NotNull] this T? value, [CallerArgumentExpression("value")] string name = "")
        where T : class => value ?? throw new ArgumentNullException(name);

    public static string NotNullOrEmpty([NotNull] this string? value, [CallerArgumentExpression("value")] string name = "")
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("The parameter is null or empty.", name);
        }

#pragma warning disable CS8777 // Parameter must have a non-null value when exiting.
        return value!;
#pragma warning restore CS8777 // Parameter must have a non-null value when exiting.
    }

    public static string NotNullOrWhitespace([NotNull] this string? value, [CallerArgumentExpression("value")] string name = "")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("The parameter is null or empty or contains only white space.", name);
        }

#pragma warning disable CS8777 // Parameter must have a non-null value when exiting.
        return value!;
#pragma warning restore CS8777 // Parameter must have a non-null value when exiting.
    }
}
