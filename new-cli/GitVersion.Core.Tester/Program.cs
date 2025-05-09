using GitVersion;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

var modules = new IGitVersionModule[]
{
    new CoreModule(),
    new LibGit2SharpCoreModule(),
};

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, _) => cts.Cancel();

await using var serviceProvider = RegisterModules(modules);
var app = serviceProvider.GetRequiredService<GitVersionApp>();
var result = await app.RunAsync(args);

if (!Console.IsInputRedirected) Console.ReadKey();

return result;

static ServiceProvider RegisterModules(IEnumerable<IGitVersionModule> gitVersionModules)
{
    var serviceProvider = new ServiceCollection()
        .RegisterModules(gitVersionModules)
        .AddSingleton<GitVersionApp>()
        .RegisterLogging()
        .BuildServiceProvider();

    return serviceProvider;
}
