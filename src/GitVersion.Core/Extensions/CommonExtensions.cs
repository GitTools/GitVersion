using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GitVersion.Extensions;

/// <summary>General-purpose extension and guard methods used throughout GitVersion.</summary>
public static class CommonExtensions
{
    /// <summary>Throws <see cref="ArgumentNullException"/> when <paramref name="value"/> is <see langword="null"/>; otherwise returns the value.</summary>
    public static T NotNull<T>([NotNull] this T? value, [CallerArgumentExpression(nameof(value))] string name = "")
        where T : class => value ?? throw new ArgumentNullException(name);
}
