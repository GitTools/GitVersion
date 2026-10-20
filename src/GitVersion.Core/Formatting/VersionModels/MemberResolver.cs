using GitVersion.OutputVariables;

namespace GitVersion.Formatting;

// The experimental CLI source-links Formatting/*.cs without the legacy version
// models. Keep this model-specific metadata graph outside that shared file set.
internal partial class MemberResolver
{
    public static MemberInfo[] ResolveVersionMemberPath(Type type, string memberExpression)
        => ResolveMemberPath(type, memberExpression, GetVersionMembers);

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
}
