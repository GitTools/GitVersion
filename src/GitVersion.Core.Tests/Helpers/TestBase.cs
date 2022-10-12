using GitVersion.Configuration;
using GitVersion.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion.Core.Tests.Helpers;

public class TestBase
{
    public const string MainBranch = "main";

    protected static IServiceProvider ConfigureServices(Action<IServiceCollection>? overrideServices = null)
    {
        var services = new ServiceCollection()
            .AddModule(new GitVersionCoreTestModule());

        overrideServices?.Invoke(services);

        return services.BuildServiceProvider();
    }

    protected static IServiceProvider BuildServiceProvider(string workingDirectory, IGitRepository repository, string branch, GitVersionConfiguration? configuration = null)
    {
        configuration ??= new ConfigurationBuilder().Build();
        var options = Options.Create(new GitVersionOptions
        {
            WorkingDirectory = workingDirectory,
            ConfigInfo = { OverrideConfig = configuration },
            RepositoryInfo = { TargetBranch = branch }
        });

        var sp = ConfigureServices(services =>
        {
            services.AddSingleton(options);
            services.AddSingleton(repository);
        });
        return sp;
    }
}
