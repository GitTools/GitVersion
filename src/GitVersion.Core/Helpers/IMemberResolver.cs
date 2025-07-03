namespace GitVersion.Helpers;

internal interface IMemberResolver
{
    MemberInfo[] ResolveMemberPath(Type type, string memberExpression);
}
