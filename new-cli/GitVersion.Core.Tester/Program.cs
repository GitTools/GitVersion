using GitVersion;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

var modules = new IGitVersionModule[]
{
    new CoreModule(),
    new ManagedCoreModule(),
};

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, _) =>
{
    cts.Cancel();
    cts.Dispose();
};

try
{
    await using var serviceProvider = RegisterModules(modules);
    var app = serviceProvider.GetRequiredService<ICliApp>();

    var result = await app.RunAsync(args, cts.Token).ConfigureAwait(false);
    if (!Console.IsInputRedirected)
    {
        Console.ReadKey();
    }

    return result;
}
catch (WarningException exception)
{
    await Console.Error.WriteLineAsync("An error occurred:").ConfigureAwait(false);
    await Console.Error.WriteLineAsync(exception.Message).ConfigureAwait(false);
    return 1;
}

static ServiceProvider RegisterModules(IEnumerable<IGitVersionModule> gitVersionModules)
{
    var serviceProvider = new ServiceCollection()
        .RegisterModules(gitVersionModules)
        .AddSingleton<ICliApp, GitVersionApp>()
        .RegisterLogging()
        .BuildServiceProvider();

    return serviceProvider;
}
