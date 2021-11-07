using System;
using System.Linq;
using GitVersion.Calculation;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Infrastructure;
using GitVersion.Normalization;
using GitVersion.Output;

namespace GitVersion;

public class ModulesLoader
{
    public static IContainer Load(string[] args)
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

        var gitVersionModules = assemblies
            .SelectMany(a => a.GetTypes().Where(t => t.TypeIsGitVersionModule()))
            .Select(t => (IGitVersionModule)Activator.CreateInstance(t)!)
            .ToList();

        var serviceProvider = new ContainerRegistrar()
            .RegisterModules(gitVersionModules)
            .AddLogging(args)
            .Build();

        return serviceProvider;
    }
}