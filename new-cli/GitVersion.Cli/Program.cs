using GitVersion;
using GitVersion.Extensions;
using GitVersion.Generated;
using GitVersion.Infrastructure;

var modules = new IGitVersionModule[]
{
    new CoreModule(),
    new LibGit2SharpCoreModule(),
    new NormalizeModule(),
    new CalculateModule(),
    new ConfigModule(),
    new OutputModule(),
};

using var serviceProvider = LoadGitVersionModules(modules);
var app = serviceProvider.GetRequiredService<GitVersionApp>();
var result = await app.RunAsync(args);

if (!Console.IsInputRedirected) Console.ReadKey();

return result;

static IContainer LoadGitVersionModules(IEnumerable<IGitVersionModule> gitVersionModules)
{
    var serviceProvider = new ContainerRegistrar()
        .RegisterModules(gitVersionModules)
        .RegisterModule(new CommandsImplModule())
        .AddSingleton<GitVersionApp>()
        .AddLogging()
        .Build();

    return serviceProvider;
}
