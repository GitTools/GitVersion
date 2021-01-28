using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace GitVersion.Helpers
{
    internal static class StringFormatWithExtension
    {
        // This regex matches an expression to replace.
        // - env:ENV name OR a member name
        // - optional fallback value after " ?? "
        // - the fallback value should be a quoted string, but simple unquoted text is allowed for back compat
        private static readonly Regex TokensRegex = new Regex(@"{((env:(?<envvar>\w+))|(?<member>\w+))(\s+(\?\?)??\s+((?<fallback>\w+)|""(?<fallback>.*)""))??}", RegexOptions.Compiled);

        /// <summary>
        /// Formats the <paramref name="template"/>, replacing each expression wrapped in curly braces
        /// with the corresponding property from the <paramref name="source"/> or <paramref name="environment"/>.
        /// </summary>
        /// <param name="template" this="true">The source template, which may contain expressions to be replaced, e.g '{Foo.Bar.CurrencySymbol} foo {Foo.Bar.Price}'</param>
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
        public static string FormatWith<T>(this string template, T source, IEnvironment environment)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            foreach (Match match in TokensRegex.Matches(template))
            {
                string propertyValue;
                string fallback = match.Groups["fallback"].Success ? match.Groups["fallback"].Value : null;

                if (match.Groups["envvar"].Success)
                {
                    string envVar = match.Groups["envvar"].Value;
                    propertyValue = environment.GetEnvironmentVariable(envVar) ?? fallback
                        ?? throw new ArgumentException($"Environment variable {envVar} not found and no fallback string provided");
                }
                else
                {
                    var objType = source.GetType();
                    string memberAccessExpression = match.Groups["member"].Value;
                    var expression = CompileDataBinder(objType, memberAccessExpression);
                    // It would be better to throw if the expression and fallback produce null, but provide an empty string for back compat.
                    propertyValue = expression(source)?.ToString() ?? fallback ?? "";
                }

                template = template.Replace(match.Value, propertyValue);
            }

            return template;
        }

        private static Func<object, object> CompileDataBinder(Type type, string expr)
        {
            ParameterExpression param = Expression.Parameter(typeof(object));
            Expression body = Expression.Convert(param, type);
            body = expr.Split('.').Aggregate(body, Expression.PropertyOrField);
            body = Expression.Convert(body, typeof(object)); // Convert result in case the body produces a Nullable value type.
            return Expression.Lambda<Func<object, object>>(body, param).Compile();
        }
    }
}
