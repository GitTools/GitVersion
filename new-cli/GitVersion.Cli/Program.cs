using GitVersion;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

var modules = new IGitVersionModule[]
{
    new CoreModule(),
    new LibGit2SharpCoreModule(),
    new GitVersion.Generated.CommandsModule()
};

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, _) =>
{
    cts.Cancel();
    cts.Dispose();
};

await using var serviceProvider = RegisterModules(modules);
var app = serviceProvider.GetRequiredService<ICliApp>();

var result = await app.RunAsync(args, cts.Token).ConfigureAwait(false);
if (!Console.IsInputRedirected) Console.ReadKey();

return result;

static ServiceProvider RegisterModules(IEnumerable<IGitVersionModule> gitVersionModules)
{
    var serviceProvider = new ServiceCollection()
        .RegisterModules(gitVersionModules)
        .RegisterLogging()
        .BuildServiceProvider();

    return serviceProvider;
}
