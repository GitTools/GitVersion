using GitVersion.Calculation;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Infrastructure;
using GitVersion.Normalization;
using GitVersion.Output;

namespace GitVersion;

public class ModulesLoader
{
    public static IContainer Load()
    {
        // TODO: load the list of assemblies from the app working directory, later we might load from nuget
        var assemblies = new[]
        {
            typeof(CommonModule).Assembly,
            typeof(NormalizeModule).Assembly,
            typeof(CalculateModule).Assembly,
            typeof(ConfigModule).Assembly,
            typeof(OutputModule).Assembly,
            typeof(CliModule).Assembly
        };

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