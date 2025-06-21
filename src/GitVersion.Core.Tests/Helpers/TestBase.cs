using GitVersion.Extensions;
using GitVersion.Git;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion.Core.Tests.Helpers;

public class TestBase
{
    public const string MainBranch = RepositoryFixtureBase.MainBranch;

    protected static IServiceProvider ConfigureServices(Action<IServiceCollection>? overrideServices = null)
    {
        var services = new ServiceCollection()
            .AddModule(new GitVersionCoreTestModule());

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
