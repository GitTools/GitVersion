using GitVersion.Extensions;
using GitVersion.Git;
using Serilog.Core;

namespace GitVersion.Core.Tests.Helpers;

public class TestBase
{
    public const string MainBranch = "main";

    protected static IServiceProvider ConfigureServices(Action<IServiceCollection>? overrideServices = null)
    {
        var services = new ServiceCollection()
            .AddModule(new GitVersionCoreTestModule())
            .AddSingleton(new LoggingLevelSwitch());

        overrideServices?.Invoke(services);

        return services.BuildServiceProvider();
    }

    protected static IServiceProvider BuildServiceProvider(IGitRepository repository,
        string? targetBranch = null, IReadOnlyDictionary<object, object?>? configuration = null)
    {
        var options = Options.Create(new GitVersionOptions
        {
            WorkingDirectory = repository.Path,
            ConfigurationInfo = { OverrideConfiguration = configuration },
            RepositoryInfo = { TargetBranch = targetBranch }
        });

        var sp = ConfigureServices(services =>
        {
            services.AddSingleton(options);
            services.AddSingleton(repository);
        });
        return sp;
    }
}
