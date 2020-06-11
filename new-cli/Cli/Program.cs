using System;
using System.Linq;
using System.Reflection;
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
            NewMethod();
            await using var serviceProvider = new ServiceCollection()
                .AddSingleton<GitVersionApp>()
                .AddModule(new CalculateModule()) // dynamically load the modules (maybe using Assembly.Load)
                .AddModule(new OutputModule())
                .BuildServiceProvider();
            
            var app = serviceProvider.GetService<GitVersionApp>();

            var result = await app.RunAsync(args);
            Console.ReadKey();

            return result;
        }

        private static void NewMethod()
        {
            var propertyInfos = typeof(GlobalOptions).GetProperties();
            foreach (var propertyInfo in propertyInfos)
            {
                var optionAttr = propertyInfo.GetCustomAttributes().Cast<OptionAttribute>().Single();
                var aliases = optionAttr.Aliases;
                var description = optionAttr.Description;
            }
        }
    }
}