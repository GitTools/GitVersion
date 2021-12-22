using GitVersion.Infrastructure;

namespace GitVersion.Extensions;

public static class ContainerRegistrarExtensions
{
    private static IContainerRegistrar RegisterModule(this IContainerRegistrar containerRegistrar,
        IGitVersionModule gitVersionModule)
    {
        gitVersionModule.RegisterTypes(containerRegistrar);
        return containerRegistrar;
    }

    internal static IContainerRegistrar RegisterModules(this IContainerRegistrar containerRegistrar,
        IEnumerable<IGitVersionModule> gitVersionModules)
    {
        foreach (var gitVersionModule in gitVersionModules)
        {
            containerRegistrar.RegisterModule(gitVersionModule);
        }

        return containerRegistrar;
    }
}