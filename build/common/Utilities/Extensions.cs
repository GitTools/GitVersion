using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cake.Core.IO;
using Cake.Frosting;

namespace Common.Utilities
{
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
        public static DirectoryPath Combine(this string path, string segment) => DirectoryPath.FromString(path).Combine(segment);
        public static FilePath CombineWithFilePath(this string path, string segment) => DirectoryPath.FromString(path).CombineWithFilePath(segment);
    }

}
