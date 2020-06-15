using System;
using System.Threading.Tasks;
using Calculate;
using Core;
using Microsoft.Extensions.DependencyInjection;
using Output;

namespace Cli
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            //NewMethod();
            await using var serviceProvider = new ServiceCollection()
                .AddSingleton<GitVersionApp>()
                .AddSingleton<IService, Service>()
                .AddModule(new CalculateModule()) // dynamically load the modules (maybe using Assembly.Load)
                .AddModule(new OutputModule())
                .BuildServiceProvider();

            var app = serviceProvider.GetService<GitVersionApp>();

            var result = await app.RunAsync(args);
            Console.ReadKey();

            return result;
        }
    }
}