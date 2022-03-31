using GitVersion.Infrastructure;

namespace GitVersion.Extensions;

public static class ContainerRegistrarExtensions
{
    public static bool TypeIsGitVersionModule(this Type type) =>
        typeof(IGitVersionModule).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract;

    public static IContainerRegistrar RegisterModules(this IContainerRegistrar containerRegistrar,
        IEnumerable<IGitVersionModule> gitVersionModules)
        => gitVersionModules.Aggregate(containerRegistrar, (current, gitVersionModule) => current.RegisterModule(gitVersionModule));

    private static IContainerRegistrar RegisterModule(this IContainerRegistrar containerRegistrar,
        IGitVersionModule gitVersionModule)
    {
        gitVersionModule.RegisterTypes(containerRegistrar);
        return containerRegistrar;
    }
}
