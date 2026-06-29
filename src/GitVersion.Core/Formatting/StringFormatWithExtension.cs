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

        var tokenizer = new LabelTokenizer(input);
        var tokens = tokenizer.ParseTokens().ToArray();

        Exception? lastException = null;

        foreach (var token in tokens)
        {
            if (token.Type == LabelTokenType.Literal)
            {
                return token.Name;
            }

            try
            {
                var value = token.Type == LabelTokenType.Environment
                    ? EvaluateEnvVar(token.Name, environment)
                    : memberEvaluator(token.Name);

                if (value is null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(token.Format) && ValueFormatter.Default.TryFormat(value, InputSanitizer.SanitizeFormat(token.Format), out var formatted))
                {
                    return formatted;
                }

                return value;
            }
            catch (Exception e)
            {
                lastException = e;
            }
        }

        if (lastException != null)
        {
            throw lastException;
        }

        return string.Empty;
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
            throw new ArgumentException($"'{safeMember}' is not a valid placeholder");

        return value?.ToString();
    }

    private static string EvaluateEnvVar(string name, IEnvironment environment)
    {
        var safeName = InputSanitizer.SanitizeEnvVarName(name);

        return environment.GetEnvironmentVariable(name) ?? throw new ArgumentException($"Environment variable {safeName} not found and no fallback provided");
    }
}
