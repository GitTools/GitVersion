using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GitVersion.Extensions;

/// <summary>General-purpose extension and guard methods used throughout GitVersion.</summary>
public static class CommonExtensions
{
    /// <summary>Throws <see cref="ArgumentNullException"/> when <paramref name="value"/> is <see langword="null"/>; otherwise returns the value.</summary>
    public static T NotNull<T>([NotNull] this T? value, [CallerArgumentExpression(nameof(value))] string name = "")
        where T : class => value ?? throw new ArgumentNullException(name);

    extension([NotNull] string? value)
    {
        /// <summary>Throws <see cref="ArgumentException"/> when <paramref name="value"/> is <see langword="null"/> or empty; otherwise returns the value.</summary>
        public string NotNullOrEmpty([CallerArgumentExpression(nameof(value))] string name = "")
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("The parameter is null or empty.", name);
            }

            return value;
        }

        /// <summary>Throws <see cref="ArgumentException"/> when <paramref name="value"/> is <see langword="null"/>, empty, or whitespace; otherwise returns the value.</summary>
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
