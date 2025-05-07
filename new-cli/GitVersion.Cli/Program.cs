using GitVersion;
using GitVersion.Extensions;
using GitVersion.Generated;
using GitVersion.Git;
using GitVersion.Infrastructure;

var modules = new IGitVersionModule[]
{
    new CoreModule(),
    new LibGit2SharpCoreModule(),
    new CommandsModule(),
    new CliModule()
};

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, _) => cts.Cancel();

using var serviceProvider = RegisterModules(modules);
var app = serviceProvider.GetRequiredService<IGitVersionAppRunner>();

var result = 0;
result = await app.RunAsync(args, cts.Token).ConfigureAwait(false);
if (!Console.IsInputRedirected) Console.ReadKey();
return result;

static IContainer RegisterModules(IEnumerable<IGitVersionModule> gitVersionModules)
{
    var serviceProvider = new ContainerRegistrar()
        .RegisterModules(gitVersionModules)
        .Build();

    return serviceProvider;
}
