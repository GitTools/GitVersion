using System;
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
            using var serviceProvider  = new ContainerRegistrar()
                .AddSingleton<GitVersionApp>()
                .AddSingleton<IService, Service>()
                .RegisterModule(new CalculateModule())
                .RegisterModule(new OutputModule())
                .Build();

            var app = serviceProvider.GetService<GitVersionApp>();

            var result = await app.RunAsync(args);

            if (!Console.IsInputRedirected)
            {
                Console.ReadKey();
            }

            return result;
        }
    }
}