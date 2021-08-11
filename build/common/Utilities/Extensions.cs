using System;
using System.Collections.Generic;
using System.IO;
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

        public static IEnumerable<string> GetDockerTagsForRepository(this BuildContextBase context, DockerImage dockerImage, string repositoryName)
        {
            var name = $"gittools/gitversion";
            var (distro, targetFramework) = dockerImage;

            if (context.Version == null) return Enumerable.Empty<string>();
            var tags = new List<string>
            {
                $"{name}:{context.Version.Version}-{distro}-{targetFramework}",
                $"{name}:{context.Version.SemVersion}-{distro}-{targetFramework}",
            };

            if (distro == Constants.DockerDistroLatest && targetFramework == Constants.Version50)
            {
                tags.AddRange(new[]
                {
                    $"{name}:{context.Version.Version}",
                    $"{name}:{context.Version.SemVersion}",

                    $"{name}:{context.Version.Version}-{distro}",
                    $"{name}:{context.Version.SemVersion}-{distro}"
                });

                if (context.IsStableRelease)
                {
                    tags.AddRange(new[]
                    {
                        $"{name}:latest",
                        $"{name}:latest-{targetFramework}",
                        $"{name}:latest-{distro}",
                        $"{name}:latest-{distro}-{targetFramework}",
                    });
                }
            }

            return tags;
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
    }
}
