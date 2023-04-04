using System.Diagnostics.CodeAnalysis;

namespace GitVersion.Extensions
{
    public static class StringExtensions
    {
        public static bool IsEquivalentTo(this string self, string? other) =>
            string.Equals(self, other, StringComparison.OrdinalIgnoreCase);

        /// <inheritdoc cref="string.IsNullOrEmpty"/>
        public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value) => string.IsNullOrEmpty(value);

        /// <inheritdoc cref="string.IsNullOrWhiteSpace"/>
        public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? value) => string.IsNullOrWhiteSpace(value);

        public static bool IsEmpty([NotNullWhen(false)] this string? value) => string.Empty.Equals(value);
    }
}
