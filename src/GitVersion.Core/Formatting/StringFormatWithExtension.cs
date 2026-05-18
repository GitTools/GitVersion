using System.Text.RegularExpressions;
using GitVersion.Core;

namespace GitVersion.Formatting;

internal static class StringFormatWithExtension
{
    private static readonly IExpressionCompiler ExpressionCompiler = new ExpressionCompiler();

    private static readonly IInputSanitizer InputSanitizer = new InputSanitizer();

    private static readonly IMemberResolver MemberResolver = new MemberResolver();

    /// <summary>
    /// Provides extension methods for formatting strings using a source object and environment context.
    /// </summary>
    extension(string template)
    {
        /// <summary>
        /// Formats the <paramref name="template"/>, replacing each expression wrapped in curly braces
        /// with the corresponding property from the <paramref name="source"/> or <paramref name="environment"/>.
        /// </summary>
        /// <param name="source">The source object to apply to the <paramref name="template"/></param>
        /// <param name="environment"></param>
        /// <exception cref="ArgumentNullException">The <paramref name="template"/> is null.</exception>
        /// <exception cref="ArgumentException">An environment variable was null and no fallback was provided.</exception>
        /// <remarks>
        /// An expression containing "." is treated as a property or field access on the <paramref name="source"/>.
        /// An expression starting with "env:" is replaced with the value of the corresponding variable from the <paramref name="environment"/>.
        /// Each expression may specify a single hardcoded fallback value using the {Prop ?? "fallback"} syntax, which applies if the expression evaluates to null.
        /// </remarks>
        /// <example>
        /// // replace an expression with a property value
        /// "Hello {Name}".FormatWith(new { Name = "Fred" }, env);
        /// "Hello {Name ?? \"Fred\"}".FormatWith(new { Name = GetNameOrNull() }, env);
        /// // replace an expression with an environment variable
        /// "{env:BUILD_NUMBER}".FormatWith(new { }, env);
        /// "{env:BUILD_NUMBER ?? \"0\"}".FormatWith(new { }, env);
        /// </example>
        public string FormatWith<T>(T? source, IEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(template);
            ArgumentNullException.ThrowIfNull(source);

            var result = new StringBuilder();
            var lastIndex = 0;

            foreach (var match in RegexPatterns.ExpandTokensRegex.Matches(template).Cast<Match>())
            {
                var replacement = EvaluateMatch(match, source, environment);
                result.Append(template, lastIndex, match.Index - lastIndex);
                result.Append(replacement);
                lastIndex = match.Index + match.Length;
            }

            result.Append(template, lastIndex, template.Length - lastIndex);
            return result.ToString();
        }
    }

    private static string EvaluateMatch<T>(Match match, T source, IEnvironment environment)
    {
        var fallback = match.Groups["fallback"].Success ? match.Groups["fallback"].Value : null;

        if (match.Groups["envvar"].Success)
            return EvaluateEnvVar(match.Groups["envvar"].Value, fallback, environment);

        if (match.Groups["member"].Success)
        {
            var format = match.Groups["format"].Success ? match.Groups["format"].Value : null;
            return EvaluateMember(source, match.Groups["member"].Value, format, fallback);
        }

        throw new ArgumentException($"Invalid token format: '{match.Value}'");
    }

    private static string EvaluateEnvVar(string name, string? fallback, IEnvironment env)
    {
        var safeName = InputSanitizer.SanitizeEnvVarName(name);
        return env.GetEnvironmentVariable(safeName)
            ?? fallback
            ?? throw new ArgumentException($"Environment variable {safeName} not found and no fallback provided");
    }

    private static string EvaluateMember<T>(T source, string member, string? format, string? fallback)
    {
        var safeMember = InputSanitizer.SanitizeMemberName(member);
        var memberPath = MemberResolver.ResolveMemberPath(source!.GetType(), safeMember);
        var getter = ExpressionCompiler.CompileGetter(source.GetType(), memberPath);
        var value = getter(source);

        // Only return early for null if format doesn't use legacy syntax
        if (value is null && !HasLegacySyntax(format))
            return fallback ?? string.Empty;

        if (format is not null && ValueFormatter.Default.TryFormat(
            value,
            InputSanitizer.SanitizeFormat(format),
            out var formatted))
        {
            return formatted;
        }

        return value?.ToString() ?? fallback ?? string.Empty;
    }

    private static bool HasLegacySyntax(string? format) =>
        !string.IsNullOrEmpty(format) && format.Contains(';') && !format.Contains("??");
}
