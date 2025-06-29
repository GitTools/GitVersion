namespace GitVersion.Helpers;

internal class MemberResolver : IMemberResolver
{
    public MemberInfo[] ResolveMemberPath(Type type, string memberExpression)
    {
        var memberNames = memberExpression.Split('.');
        var path = new List<MemberInfo>();
        var currentType = type;

        foreach (var memberName in memberNames)
        {
            var member = FindDirectMember(currentType, memberName);
            if (member == null)
            {
                var recursivePath = FindMemberRecursive(type, memberName, []);
                return recursivePath == null
                    ? throw new ArgumentException($"'{memberName}' is not a property or field on type '{type.Name}'")
                    : [.. recursivePath];
            }

            path.Add(member);
            currentType = GetMemberType(member);
        }

        return [.. path];
    }

    public static List<MemberInfo>? FindMemberRecursive(Type type, string memberName, HashSet<Type> visited)
    {
        if (!visited.Add(type))
        {
            return null;
        }

        var member = FindDirectMember(type, memberName);
        if (member != null)
        {
            return [member];
        }

        foreach (var prop in type.GetProperties())
        {
            var nestedPath = FindMemberRecursive(prop.PropertyType, memberName, visited);
            if (nestedPath != null)
            {
                nestedPath.Insert(0, prop);
                return nestedPath;
            }
        }

        foreach (var field in type.GetFields())
        {
            var nestedPath = FindMemberRecursive(field.FieldType, memberName, visited);
            if (nestedPath != null)
            {
                nestedPath.Insert(0, field);
                return nestedPath;
            }
        }

        return null;
    }

    private static MemberInfo? FindDirectMember(Type type, string memberName)
        => type.GetProperty(memberName) ?? (MemberInfo?)type.GetField(memberName);

    private static Type GetMemberType(MemberInfo member) => member switch
    {
        PropertyInfo p => p.PropertyType,
        FieldInfo f => f.FieldType,
        _ => throw new ArgumentException($"Unsupported member type: {member.GetType()}")
    };
}
