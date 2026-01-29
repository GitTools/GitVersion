using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GitVersion.Extensions;

public static class CommonExtensions
{
    public static T NotNull<T>([NotNull] this T? value, [CallerArgumentExpression(nameof(value))] string name = "")
        where T : class => value ?? throw new ArgumentNullException(name);

    extension([NotNull] string? value)
    {
        public string NotNullOrEmpty([CallerArgumentExpression(nameof(value))] string name = "")
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("The parameter is null or empty.", name);
            }

            return value;
        }

        public string NotNullOrWhitespace([CallerArgumentExpression(nameof(value))] string name = "")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("The parameter is null or empty or contains only white space.", name);
            }

            return value;
        }
    }
}
