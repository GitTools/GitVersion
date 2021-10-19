using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion.Core.Tests.Helpers;

public class TestBase
{
    protected const string NoMonoDescription = "Won't run on Mono due to source information not being available for ShouldMatchApproved.";
    protected const string NoMono = "NoMono";
    protected const string NoNet48 = "NoNet48";
    public const string MainBranch = "main";

    protected static IServiceProvider ConfigureServices(Action<IServiceCollection> overrideServices = null)
    {
        var services = new ServiceCollection()
            .AddModule(new GitVersionCoreTestModule());

        overrideServices?.Invoke(services);

        return services.BuildServiceProvider();
    }

    protected static IServiceProvider BuildServiceProvider(string workingDirectory, IGitRepository repository, string branch, Config config = null)
    {
        config ??= new ConfigurationBuilder().Build();
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
