using System;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersionCore.Tests.Helpers
{
    public class TestBase
    {
        protected const string NoMonoDescription = "Won't run on Mono due to source information not being available for ShouldMatchApproved.";
        protected const string NoMono = "NoMono";

        protected static IServiceProvider ConfigureServices(Action<IServiceCollection> overrideServices = null)
        {
            var services = new ServiceCollection()
                .AddModule(new GitVersionCoreTestModule());

            overrideServices?.Invoke(services);

            return services.BuildServiceProvider();
        }

        protected static IServiceProvider BuildServiceProvider(string workingDirectory, IRepository repository, string branch, Config config = null)
        {
            config ??= new Config().ApplyDefaults();
            var options = Options.Create(new GitVersionOptions
            {
                WorkingDirectory = workingDirectory,
                ConfigInfo = { OverrideConfig = config },
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
}
