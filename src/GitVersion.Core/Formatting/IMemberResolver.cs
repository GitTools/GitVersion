namespace GitVersion.Formatting;

internal interface IMemberResolver
{
    MemberInfo[] ResolveMemberPath(Type type, string memberExpression);
}
