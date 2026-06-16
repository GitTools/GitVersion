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

            return template.FormatWith(member => EvaluateMemberFromObject(source, member), environment);
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

            return template.FormatWith(member => EvaluateMemberFromDictionary(source, member), environment);
        }

        private string FormatWith(Func<string, string?> memberEvaluator, IEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(template);

            return RegexPatterns.ExpandTokensRegex.Replace(template, match => EvaluateMatch(match.Groups[1].Value, memberEvaluator, environment));
        }
    }

    private static string EvaluateMatch(string input, Func<string, string?> memberEvaluator, IEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(input);

        foreach (var token in ParseTokens(input))
        {
            if (token.Type == TokenType.Literal)
            {
                return token.Name;
            }

            var value = token.Type == TokenType.EnvironmentVariable
                ? environment.GetEnvironmentVariable(token.Name)
                : memberEvaluator(token.Name);

            if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(token.Format))
            {
                if (ValueFormatter.Default.TryFormat(value, InputSanitizer.SanitizeFormat(token.Format), out var formatted))
                {
                    return formatted;
                }

                return value;
            }

            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        throw new ArgumentException($"Invalid token string or no available values to parse: '{input}'");
    }

    private static string? EvaluateMemberFromObject(object source, string member)
    {
        var safeMember = InputSanitizer.SanitizeMemberName(member);
        var memberPath = MemberResolver.ResolveMemberPath(source.GetType(), safeMember);
        var getter = ExpressionCompiler.CompileGetter(source.GetType(), memberPath);

        var value = getter(source);

        return value?.ToString();
    }

    private static string? EvaluateMemberFromDictionary(IDictionary<string, object> source, string member)
    {
        var safeMember = InputSanitizer.SanitizeMemberName(member);

        if (!source.TryGetValue(safeMember, out var value))
            return null;

        return value.ToString();
    }

    private static IEnumerable<Token> ParseTokens(string value)
    {
        var tokens = new List<Token>();
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
                tokens.Add(ParseToken(current.ToString()));

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
            tokens.Add(ParseToken(current.ToString()));
        }

        return tokens;
    }

    private static Token ParseToken(string token)
    {
        token = token.Trim();

        if (token.StartsWith('"') && token.EndsWith('"'))
        {
            return new Token(token.Trim('"'), TokenType.Literal);
        }

        if (token.StartsWith("env:", StringComparison.OrdinalIgnoreCase))
        {
            var variable = token[4..];

            var (name, format) = ParseNameAndFormat(variable);
            var safeName = InputSanitizer.SanitizeEnvVarName(name);

            return new Token(safeName, TokenType.EnvironmentVariable, format);
        }

        var (member, memberFormat) = ParseNameAndFormat(token);

        return new Token(member, TokenType.Proeprty, memberFormat);
    }

    private static (string Name, string? Format) ParseNameAndFormat(string value)
    {
        if (value.Split(':', 2) is [var name, var format])
        {
            return (name, format);
        }

        return (value, null);
    }

    private static string UnescapeLiteral(string value) => value.Replace("\\\"", "\"");

    private enum TokenType
    {
        Literal,
        Proeprty,
        EnvironmentVariable
    }

    private record Token(string Name, TokenType Type, string? Format = null);
}
