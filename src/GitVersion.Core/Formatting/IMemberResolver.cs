using System.Diagnostics.CodeAnalysis;

namespace GitVersion.Formatting;

internal interface IMemberResolver
{
    [RequiresUnreferencedCode("Arbitrary object formatting recursively discovers public properties and fields.")]
    MemberInfo[] ResolveMemberPath(Type type, string memberExpression);
}
