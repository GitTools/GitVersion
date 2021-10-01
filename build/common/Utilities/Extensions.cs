namespace Common.Utilities;

public static class Extensions
{
    public static IEnumerable<Type> FindAllDerivedTypes(this Assembly assembly, Type baseType) =>
        from type in assembly.GetExportedTypes()
        let info = type.GetTypeInfo()
        where baseType.IsAssignableFrom(type) && info.IsClass && !info.IsAbstract
        select type;

    public static string GetTaskDescription(this Type task)
    {
        if (task is null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        var attribute = task.GetCustomAttribute<TaskDescriptionAttribute>();
        return attribute != null ? attribute.Description : string.Empty;
    }

    public static string GetTaskName(this Type task)
    {
        if (task is null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        var attribute = task.GetCustomAttribute<TaskNameAttribute>();
        return attribute != null ? attribute.Name : task.Name;
    }

    public static string GetTaskArguments(this Type task)
    {
        if (task is null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        var attributes = task.GetCustomAttributes<TaskArgumentAttribute>().ToArray();
        if (attributes.Any())
        {
            var arguments = attributes.Select(attribute => $"[--{attribute.Name} ({string.Join(" | ", attribute.PossibleValues)})]");
            return string.Join(" ", arguments);
        }
        return string.Empty;
    }

    public static DirectoryPath Combine(this string path, string segment) => DirectoryPath.FromString(path).Combine(segment);
    public static FilePath CombineWithFilePath(this string path, string segment) => DirectoryPath.FromString(path).CombineWithFilePath(segment);
    public static DirectoryPath GetRootDirectory()
    {
        var currentPath = DirectoryPath.FromString(Directory.GetCurrentDirectory());
        while (!Directory.Exists(currentPath.Combine(".git").FullPath))
        {
            currentPath = DirectoryPath.FromString(Directory.GetParent(currentPath.FullPath)?.FullName);
        }

        return currentPath;
    }

    public static string ToSuffix(this Architecture arch) => arch.ToString().ToLower();
}
