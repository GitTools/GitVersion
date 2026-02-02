namespace Common.Utilities;

#pragma warning disable S1144
public static class Extensions
{
    private static readonly char[] CharsThatRequireQuoting = [' ', '"'];
    private static readonly char[] CharsThatRequireEscaping = ['\\', '"'];

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

    extension(string literalValue)
    {
        /// <summary>
        /// Escapes arbitrary values so that the process receives the exact string you intend and injection is impossible.
        /// Spec: https://msdn.microsoft.com/en-us/library/bb776391.aspx
        /// </summary>
        public string EscapeProcessArgument(bool alwaysQuote = false)
        {
            if (string.IsNullOrEmpty(literalValue)) return "\"\"";

            if (literalValue.IndexOfAny(CharsThatRequireQuoting) == -1) // Happy path
            {
                if (!alwaysQuote) return literalValue;
                if (literalValue[^1] != '\\') return "\"" + literalValue + "\"";
            }

            var sb = new StringBuilder(literalValue.Length + 8).Append('"');

            var nextPosition = 0;
            while (true)
            {
                var nextEscapeChar = literalValue.IndexOfAny(CharsThatRequireEscaping, nextPosition);
                if (nextEscapeChar == -1) break;

                sb.Append(literalValue, nextPosition, nextEscapeChar - nextPosition);
                nextPosition = nextEscapeChar + 1;

                switch (literalValue[nextEscapeChar])
                {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        var numBackslashes = 1;
                        while (nextPosition < literalValue.Length && literalValue[nextPosition] == '\\')
                        {
                            numBackslashes++;
                            nextPosition++;
                        }
                        if (nextPosition == literalValue.Length || literalValue[nextPosition] == '"')
                            numBackslashes <<= 1;

                        for (; numBackslashes != 0; numBackslashes--)
                            sb.Append('\\');
                        break;
                }
            }

            sb.Append(literalValue, nextPosition, literalValue.Length - nextPosition).Append('"');
            return sb.ToString();
        }
    }
}
#pragma warning restore S1144
