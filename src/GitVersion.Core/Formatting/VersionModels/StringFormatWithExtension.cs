using GitVersion.OutputVariables;

namespace GitVersion.Formatting;

// These overloads belong to the legacy CLI model graph, not the formatter sources
// linked into GitVersion.Common by the experimental CLI.
internal static partial class StringFormatWithExtension
{
    extension(string template)
    {
        /// <summary>Formats computed version values using their statically known public member graph.</summary>
        public string FormatWith(SemanticVersionFormatValues source, IEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(source);
            return template.FormatWith(member => EvaluateVersionMember(source, typeof(SemanticVersionFormatValues), member), environment);
        }

        /// <summary>Formats output variables using their statically known public member graph.</summary>
        public string FormatWith(GitVersionVariables source, IEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(source);
            return template.FormatWith(member => EvaluateVersionMember(source, typeof(GitVersionVariables), member), environment);
        }
    }

    private static string? EvaluateVersionMember(object source, Type type, string member)
    {
        var safeMember = InputSanitizer.SanitizeMemberName(member);
        var memberPath = MemberResolver.ResolveVersionMemberPath(type, safeMember);
        return ExpressionCompiler.CompileGetter(type, memberPath)(source)?.ToString();
    }
}
