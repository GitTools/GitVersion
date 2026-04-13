using System.Buffers;

namespace Common.Utilities;

#pragma warning disable S1144
public static class Extensions
{
    private static readonly SearchValues<char> CharsRequiringQuoting = SearchValues.Create(' ', '"');
    private static readonly SearchValues<char> CharsRequiringEscaping = SearchValues.Create('\\', '"');

    extension(Assembly assembly)
    {
        public IEnumerable<Type> FindAllDerivedTypes(Type baseType) =>
            from type in assembly.GetExportedTypes()
            let info = type.GetTypeInfo()
            where baseType.IsAssignableFrom(type) && info.IsClass && !info.IsAbstract
            select type;
    }

    extension(Type task)
    {
        public string GetTaskDescription()
        {
            ArgumentNullException.ThrowIfNull(task);

            var attribute = task.GetCustomAttribute<TaskDescriptionAttribute>();
            return attribute != null ? attribute.Description : string.Empty;
        }

        public string GetTaskName()
        {
            ArgumentNullException.ThrowIfNull(task);

            var attribute = task.GetCustomAttribute<TaskNameAttribute>();
            return attribute != null ? attribute.Name : task.Name;
        }

        public string GetTaskArguments()
        {
            ArgumentNullException.ThrowIfNull(task);

            var attributes = task.GetCustomAttributes<TaskArgumentAttribute>().ToArray();
            if (attributes.Length != 0)
            {
                var arguments = attributes.Select(attribute => $"[--{attribute.Name} ({string.Join(" | ", attribute.PossibleValues)})]");
                return string.Join(" ", arguments);
            }
            return string.Empty;
        }
    }

    public static DirectoryPath GetRootDirectory()
    {
        var currentPath = DirectoryPath.FromString(Directory.GetCurrentDirectory());
        while (!Directory.Exists(currentPath.Combine(".git").FullPath))
        {
            currentPath = currentPath.GetParent();
        }

        return currentPath;
    }

    extension(Architecture arch)
    {
        public string ToSuffix() => arch.ToString().ToLower();
    }

    extension(string value)
    {
        public bool IsNullOrWhiteSpace() =>
            string.IsNullOrWhiteSpace(value);

        public bool IsEqualInvariant(string other) =>
            string.Equals(value, other, StringComparison.InvariantCulture);
    }

    /// <summary>
    /// Escapes arbitrary values so that the process receives the exact string you intend and injection is impossible.
    /// Spec: https://msdn.microsoft.com/en-us/library/bb776391.aspx
    /// </summary>
    public static string EscapeProcessArgument(this string literalValue, bool alwaysQuote = false)
    {
        if (string.IsNullOrEmpty(literalValue)) return "\"\"";

        if (literalValue.AsSpan().IndexOfAny(CharsRequiringQuoting) == -1) // Happy path
        {
            if (!alwaysQuote) return literalValue;
            if (literalValue[^1] != '\\') return $"\"{literalValue}\"";
        }

        return BuildEscapedArgument(literalValue);
    }

    private static string BuildEscapedArgument(string s)
    {
        var sb = new StringBuilder(s.Length + 8).Append('"');
        var nextPosition = 0;

        while (true)
        {
            var relativeIndex = s.AsSpan(nextPosition).IndexOfAny(CharsRequiringEscaping);
            if (relativeIndex == -1) break;

            var nextEscapeChar = nextPosition + relativeIndex;
            sb.Append(s, nextPosition, relativeIndex);
            nextPosition = nextEscapeChar + 1;

            if (s[nextEscapeChar] == '"')
                sb.Append("\\\"");
            else
                nextPosition = AppendEscapedBackslashes(sb, s, nextPosition);
        }

        return sb.Append(s, nextPosition, s.Length - nextPosition).Append('"').ToString();
    }

    private static int AppendEscapedBackslashes(StringBuilder sb, string s, int nextPosition)
    {
        var numBackslashes = 1;
        while (nextPosition < s.Length && s[nextPosition] == '\\')
        {
            numBackslashes++;
            nextPosition++;
        }
        if (nextPosition == s.Length || s[nextPosition] == '"')
            numBackslashes <<= 1;

        sb.Append('\\', numBackslashes);
        return nextPosition;
    }
}
#pragma warning restore S1144
