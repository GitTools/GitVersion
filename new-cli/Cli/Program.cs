using System;
using System.Linq;
using System.Threading.Tasks;
using Calculate;
using Core;
using Output;

namespace Cli
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            // TODO: load the list of assemblies from the app working directory
            var assemblies = new[]
            {
                typeof(CoreModule).Assembly, 
                typeof(CalculateModule).Assembly, 
                typeof(OutputModule).Assembly
            };

            var gitVersionModules = assemblies
                .SelectMany(a => a.GetTypes().Where(t => typeof(IGitVersionModule).IsAssignableFrom(t) && !t.IsInterface))
                .Select(t => (IGitVersionModule)Activator.CreateInstance(t))
                .ToList();

            using var serviceProvider = new ContainerRegistrar()
                .RegisterModules(gitVersionModules)
                .AddSingleton<GitVersionApp>()
                .Build();

            var app = serviceProvider.GetService<GitVersionApp>();

            var result = await app.RunAsync(args);

            if (!Console.IsInputRedirected) Console.ReadKey();

            return result;
        }
    }
}