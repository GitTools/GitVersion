using System.Linq.Expressions;

namespace GitVersion.Helpers;

internal class ExpressionCompiler : IExpressionCompiler
{
    public Func<object, object?> CompileGetter(Type type, MemberInfo[] memberPath)
    {
        var param = Expression.Parameter(typeof(object));
        Expression body = Expression.Convert(param, type);

        foreach (var member in memberPath)
        {
            body = Expression.PropertyOrField(body, member.Name);
        }

        body = Expression.Convert(body, typeof(object));
        return Expression.Lambda<Func<object, object?>>(body, param).Compile();
    }
}
