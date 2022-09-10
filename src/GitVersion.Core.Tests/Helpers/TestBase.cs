using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion.Core.Tests.Helpers;

public class TestBase
{
    protected const string NoMonoDescription = "Won't run on Mono due to source information not being available for ShouldMatchApproved.";
    protected const string NoMono = "NoMono";
    protected const string NoNet48 = "NoNet48";
    public const string MainBranch = "main";

    protected static IServiceProvider ConfigureServices(Action<IServiceCollection>? overrideServices = null)
    {
        var services = new ServiceCollection()
            .AddModule(new GitVersionCoreTestModule());

        overrideServices?.Invoke(services);

        return services.BuildServiceProvider();
    }

    protected static IServiceProvider BuildServiceProvider(string workingDirectory, IGitRepository repository, string branch, Config? config = null)
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

    protected static class Configurations
    {
        public static Config ContinuousDelivery => new()
        {
            VersioningMode = VersioningMode.ContinuousDelivery
        };

        public static Config ContinuousDeliveryWithoutTrackMergeTarget => new()
        {
            VersioningMode = VersioningMode.ContinuousDelivery,
            Branches = new Dictionary<string, BranchConfig>()
        {
            { "main", new BranchConfig() { TrackMergeTarget = false } },
            { "develop", new BranchConfig() { TrackMergeTarget = false } },
            { "support", new BranchConfig() { TrackMergeTarget = false } }
        }
        };

        public static Config ContinuousDeployment => new()
        {
            VersioningMode = VersioningMode.ContinuousDeployment
        };

        public static Config ContinuousDeploymentWithoutTrackMergeTarget => new()
        {
            VersioningMode = VersioningMode.ContinuousDeployment,
            Branches = new Dictionary<string, BranchConfig>()
        {
            { "main", new BranchConfig() { TrackMergeTarget = false } },
            { "develop", new BranchConfig() { TrackMergeTarget = false } },
            { "support", new BranchConfig() { TrackMergeTarget = false } }
        }
        };

        public static Config Mainline => new()
        {
            VersioningMode = VersioningMode.Mainline
        };

        public static Config MainlineWithoutTrackMergeTarget => new()
        {
            VersioningMode = VersioningMode.Mainline,
            Branches = new Dictionary<string, BranchConfig>()
        {
            { "main", new BranchConfig() { TrackMergeTarget = false } },
            { "develop", new BranchConfig() { TrackMergeTarget = false } },
            { "support", new BranchConfig() { TrackMergeTarget = false } }
        }
        };
    }
}
