using System.Diagnostics.CodeAnalysis;
using GitVersion.OutputVariables;

namespace GitVersion.Formatting;

internal class MemberResolver : IMemberResolver
{
    [RequiresUnreferencedCode("Arbitrary object formatting recursively discovers public properties and fields.")]
    public MemberInfo[] ResolveMemberPath(Type type, string memberExpression)
        => ResolveMemberPath(type, memberExpression, GetMembers);

    public static MemberInfo[] ResolveVersionMemberPath(Type type, string memberExpression)
        => ResolveMemberPath(type, memberExpression, GetVersionMembers);

    private static MemberInfo[] ResolveMemberPath(Type type, string memberExpression, Func<Type, MemberInfo[]> getMembers)
    {
        var memberNames = memberExpression.Split('.');
        var path = new List<MemberInfo>();
        var currentType = type;

        foreach (var memberName in memberNames)
        {
            var member = FindDirectMember(getMembers(currentType), memberName);
            if (member == null)
            {
                var recursivePath = FindMemberRecursive(type, memberName, [], getMembers);
                return recursivePath == null
                    ? throw new ArgumentException($"'{memberName}' is not a property or field on type '{type.Name}'")
                    : [.. recursivePath];
            }

            path.Add(member);
            currentType = GetMemberType(member);
        }

        return [.. path];
    }

    [RequiresUnreferencedCode("Arbitrary object formatting recursively discovers public properties and fields.")]
    public static List<MemberInfo>? FindMemberRecursive(Type type, string memberName, HashSet<Type> visited)
        => FindMemberRecursive(type, memberName, visited, GetMembers);

    private static List<MemberInfo>? FindMemberRecursive(Type type, string memberName, HashSet<Type> visited, Func<Type, MemberInfo[]> getMembers)
    {
        if (!visited.Add(type))
        {
            return null;
        }

        var members = getMembers(type);
        var member = FindDirectMember(members, memberName);
        if (member != null)
        {
            return [member];
        }

        foreach (var prop in members.OfType<PropertyInfo>())
        {
            var nestedPath = FindMemberRecursive(prop.PropertyType, memberName, visited, getMembers);
            if (nestedPath != null)
            {
                nestedPath.Insert(0, prop);
                return nestedPath;
            }
        }

        foreach (var field in members.OfType<FieldInfo>())
        {
            var nestedPath = FindMemberRecursive(field.FieldType, memberName, visited, getMembers);
            if (nestedPath != null)
            {
                nestedPath.Insert(0, field);
                return nestedPath;
            }
        }

        return null;
    }

    private static MemberInfo? FindDirectMember(MemberInfo[] members, string memberName)
        => members.OfType<PropertyInfo>().SingleOrDefault(member => member.Name == memberName)
           ?? (MemberInfo?)members.OfType<FieldInfo>().SingleOrDefault(member => member.Name == memberName);

    [RequiresUnreferencedCode("Arbitrary object formatting recursively discovers public properties and fields.")]
    private static MemberInfo[] GetMembers(Type type) => [.. type.GetProperties(), .. type.GetFields()];

    // Both executable formatting models contain only strings. Keep their complete public
    // member graph explicit so nested expressions (including string.Length and Empty)
    // survive trimming without preserving unrelated application types.
    private static MemberInfo[] GetVersionMembers(Type type)
    {
        if (type == typeof(SemanticVersionFormatValues))
        {
            return [.. typeof(SemanticVersionFormatValues).GetProperties(), .. typeof(SemanticVersionFormatValues).GetFields()];
        }
        if (type == typeof(GitVersionVariables))
        {
            return [.. typeof(GitVersionVariables).GetProperties(), .. typeof(GitVersionVariables).GetFields()];
        }
        if (type == typeof(string))
        {
            return [.. typeof(string).GetProperties(), .. typeof(string).GetFields()];
        }
        if (type == typeof(int))
        {
            return [.. typeof(int).GetProperties(), .. typeof(int).GetFields()];
        }
        if (type == typeof(char))
        {
            return [.. typeof(char).GetProperties(), .. typeof(char).GetFields()];
        }

        throw new ArgumentException($"Unsupported version formatting member type: {type}");
    }

    private static Type GetMemberType(MemberInfo member) => member switch
    {
        PropertyInfo p => p.PropertyType,
        FieldInfo f => f.FieldType,
        _ => throw new ArgumentException($"Unsupported member type: {member.GetType()}")
    };
}
