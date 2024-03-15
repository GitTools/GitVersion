using GitVersion;
using GitVersion.Extensions;
using GitVersion.Generated;
using GitVersion.Infrastructure;

var modules = new IGitVersionModule[]
{
    new CoreModule(),
    new LibGit2SharpCoreModule(),
    new CommandsImplModule()
};

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, _) => cts.Cancel();

using var serviceProvider = RegisterModules(modules);
var app = serviceProvider.GetRequiredService<GitVersionApp>();
var result = await app.RunAsync(args, cts.Token).ConfigureAwait(false);

if (!Console.IsInputRedirected) Console.ReadKey();

return result;

static IContainer RegisterModules(IEnumerable<IGitVersionModule> gitVersionModules)
{
    var serviceProvider = new ContainerRegistrar()
        .RegisterModules(gitVersionModules)
        .AddSingleton<GitVersionApp>()
        .AddLogging()
        .Build();

    return serviceProvider;
}
