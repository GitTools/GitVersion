using System;
using GitTools.Testing;
using GitVersion;
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
            var arguments = new Arguments
            {
                Authentication = new AuthenticationInfo(),
                TargetPath = fixture.LocalRepositoryFixture.RepositoryPath
            };
            var options = Options.Create(arguments);

            var serviceProvider = ConfigureServices(services =>
            {
                services.AddSingleton(options);
            });

            var gitPreparer = serviceProvider.GetService<IGitPreparer>() as GitPreparer;
            gitPreparer?.PrepareInternal(true, null);
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
