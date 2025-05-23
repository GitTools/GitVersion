using GitVersion.Agents;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion.MsBuild.Tests.Helpers;

public static class GitRepositoryTestingExtensions
{
    /// <summary>
    /// Simulates running on build server
    /// </summary>
    public static void InitializeRepo(this RemoteRepositoryFixture fixture)
    {
        var gitVersionOptions = new GitVersionOptions
        {
            WorkingDirectory = fixture.LocalRepositoryFixture.RepositoryPath
        };
        var options = Options.Create(gitVersionOptions);

        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(AzurePipelines.EnvironmentVariableName, "true");

        var serviceProvider = ConfigureServices(services =>
        {
            services.AddSingleton(options);
            services.AddSingleton(environment);
        });

        var gitPreparer = serviceProvider.GetRequiredService<IGitPreparer>();
        gitPreparer.Prepare();
    }

    private static ServiceProvider ConfigureServices(Action<IServiceCollection>? servicesOverrides = null)
    {
        var services = new ServiceCollection()
            .AddModule(new GitVersionCoreTestModule());

        servicesOverrides?.Invoke(services);
        return services.BuildServiceProvider();
    }
}
