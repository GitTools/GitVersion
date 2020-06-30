using System;
using GitTools.Testing;
using GitVersion;
using GitVersion.BuildAgents;
using GitVersion.Extensions;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersionTask.Tests.Helpers
{
    public static class GitToolsTestingExtensions
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

            var gitPreparer = serviceProvider.GetService<IGitPreparer>();
            gitPreparer.Prepare();
        }

        private static IServiceProvider ConfigureServices(Action<IServiceCollection> servicesOverrides = null)
        {
            var services = new ServiceCollection()
                .AddModule(new GitVersionCoreTestModule());

            servicesOverrides?.Invoke(services);
            return services.BuildServiceProvider();
        }
    }
}
