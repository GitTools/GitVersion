using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace GitVersion
{

    static class StringFormatWithExtension
    {
        private static readonly Regex TokensRegex = new Regex(@"{\w+}", RegexOptions.Compiled);

        /// <summary>
        ///     Formats a string template with the given source object.
        ///     Expression like {Id} are replaced with the corresponding
        ///     property value in the <paramref name="source" />. 
        ///     Supports property access expressions.
        /// </summary>       
        /// <param name="template" this="true">The template to be replaced with values from the source object. The template can contain expressions wrapped in curly braces, that point to properties or fields on the source object to be used as a substitute, e.g '{Foo.Bar.CurrencySymbol} foo {Foo.Bar.Price}'.</param>
        /// <param name="source">The source object to apply to format</param>
        public static string FormatWith<T>(this string template, T source)
        {
            if (template == null)
            {
                throw new ArgumentNullException("template");
            }

            // {MajorMinorPatch}+{Branch}
            var objType = source.GetType();
            foreach (Match match in TokensRegex.Matches(template))
            {
                var memberAccessExpression = TrimBraces(match.Value);
                Func<object, string> expression = CompileDataBinder(objType, memberAccessExpression);
                string propertyValue = expression(source);
                template = template.Replace(match.Value, propertyValue);
            }

            return template;

        }


        static string TrimBraces(string originalExpression)
        {
            if (!string.IsNullOrWhiteSpace(originalExpression))
            {
                return originalExpression.TrimStart('{').TrimEnd('}');
            }
            return originalExpression;
        }

        static Func<object, string> CompileDataBinder(Type type, string expr)
        {
            var param = Expression.Parameter(typeof(object));
            Expression body = Expression.Convert(param, type);
            var members = expr.Split('.');
            for (int i = 0; i < members.Length; i++)
            {
                body = Expression.PropertyOrField(body, members[i]);
            }

            var staticOrPublic = BindingFlags.Static | BindingFlags.Public;
            var method = GetMethodInfo("ToString", staticOrPublic, new Type[] { body.Type });
            if (method == null)
            {
                method = GetMethodInfo("ToString", staticOrPublic, new Type[] { typeof(object) });
                body = Expression.Call(method, Expression.Convert(body, typeof(object)));
            }
            else
            {
                body = Expression.Call(method, body);
            }

            return Expression.Lambda<Func<object, string>>(body, param).Compile();
        }

        private static MethodInfo GetMethodInfo(string name, BindingFlags bindingFlags, Type[] types)
        {
#if NETDESKTOP
            var methodInfo = typeof(Convert).GetMethod(name, bindingFlags, null, types, null);
            return methodInfo;
#else
            var type = typeof(Convert);
            var methodInfo = typeof(Convert).GetMethod(name, types);
             return methodInfo;
#endif
            //throw new NotImplementedException();
        }
    }
}