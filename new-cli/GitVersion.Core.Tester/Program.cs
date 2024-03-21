using GitVersion;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Infrastructure;

var assemblies = new IGitVersionModule[]
{
    new CoreModule(),
    new LibGit2SharpCoreModule(),
};

using var serviceProvider = RegisterModules(assemblies);
var app = serviceProvider.GetRequiredService<GitVersionApp>();
var result = await app.RunAsync(args);

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
