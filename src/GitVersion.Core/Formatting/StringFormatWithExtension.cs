using System.Text.RegularExpressions;
using System.Xml.Linq;

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
        public string FormatWith(object source, IEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(source);

            return template.FormatWith((member, format) => EvaluateMemberFromObject(source, member, format), environment);
        }

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
        public string FormatWith(IDictionary<string, object> source, IEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(source);

            return template.FormatWith((member, format) => EvaluateMemberFromDictionary(source, member, format), environment);
        }

        private string FormatWith(EvaluateMemberDelegate memberEvaluator, IEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(template);

            return RegexPatterns.ExpandTokensRegex.Replace(template, match => EvaluateMatch(match.Groups[1].Value, memberEvaluator, environment));
        }
    }

    private static string EvaluateMatch(string input, EvaluateMemberDelegate memberEvaluator, IEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(input);

        foreach (var token in ParseFormatTokens(input))
        {
            if (token.StartsWith("env:", StringComparison.OrdinalIgnoreCase))
            {
                var safeName = InputSanitizer.SanitizeEnvVarName(token[4..]);
                var value = environment.GetEnvironmentVariable(safeName);

                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            else if (token.StartsWith('"') && token.EndsWith('"'))
            {
                return token.Trim('"');
            }
            else
            {
                var formattedParts = token.Split(':', 2);
                var member = formattedParts.First();
                var format = formattedParts.Skip(1).FirstOrDefault();

                var value = memberEvaluator(member, format);

                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
        }

        throw new ArgumentException($"Invalid token string or no available values to parse: '{input}'");
    }

    private static string? EvaluateMemberFromObject(object source, string member, string? format)
    {
        var safeMember = InputSanitizer.SanitizeMemberName(member);
        var memberPath = MemberResolver.ResolveMemberPath(source.GetType(), safeMember);
        var getter = ExpressionCompiler.CompileGetter(source.GetType(), memberPath);
        var value = getter(source);

        if (value is null)
            return null;

        if (format is not null && ValueFormatter.Default.TryFormat(
                value,
                InputSanitizer.SanitizeFormat(format),
                out var formatted))
        {
            return formatted;
        }

        return value.ToString();
    }

    private static string? EvaluateMemberFromDictionary(IDictionary<string, object> source, string member, string? format)
    {
        var safeMember = InputSanitizer.SanitizeMemberName(member);

        if (!source.TryGetValue(safeMember, out var value))
            return null;

        if (value is null)
            return null;

        if (format is not null && ValueFormatter.Default.TryFormat(value, InputSanitizer.SanitizeFormat(format), out var formatted))
            return formatted;

        return value.ToString();
    }

    private static IEnumerable<string> ParseFormatTokens(string value)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();

        var inQuotes = false;
        var index = 0;

        while (index < value.Length)
        {
            if (value[index] == '"')
            {
                inQuotes = !inQuotes;
            }

            if (!inQuotes && index + 1 < value.Length && value[index] == '?' && value[index + 1] == '?')
            {
                tokens.Add(current.ToString().Trim());

                current.Clear();
                index += 2;
            }
            else
            {
                current.Append(value[index]);
                index++;
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString().Trim());
        }

        return tokens;
    }

    private static string UnescapeLiteral(string value) => value.Replace("\\\"", "\"");

    private delegate string? EvaluateMemberDelegate(string member, string? format);
}
