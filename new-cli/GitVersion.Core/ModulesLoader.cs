using System.Reflection;
using GitVersion.Extensions;
using GitVersion.Infrastructure;

namespace GitVersion;

public class ModulesLoader
{
    public static IContainer Load(IEnumerable<Assembly> assemblies)
    {

        var gitVersionModules = new List<IGitVersionModule>();
        foreach (var type in assemblies.SelectMany(x => x.GetTypes()).Where(t => t.TypeIsGitVersionModule()))
        {
            if (Activator.CreateInstance(type) is IGitVersionModule module)
            {
                gitVersionModules.Add(module);
            }
        }

        var serviceProvider = new ContainerRegistrar()
            .RegisterModules(gitVersionModules)
            .AddLogging()
            .Build();

        return serviceProvider;
    }
}
