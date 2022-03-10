using GitVersion.Infrastructure;

namespace GitVersion.Extensions;

public static class CommonExtensions
{
    public static bool TypeIsGitVersionModule(this Type type) =>
        typeof(IGitVersionModule).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract;
}
