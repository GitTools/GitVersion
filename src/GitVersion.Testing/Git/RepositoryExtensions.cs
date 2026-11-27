using GitVersion.Git;

using GitVersion.Testing.Git;

namespace GitVersion.Testing;

public static class RepositoryExtensions
{
    public static IGitRepository ToGitRepository(this IRepository repository)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        new GitVersionManagedGitModule().RegisterTypes(services);
        var result = services.BuildServiceProvider().GetRequiredService<IGitRepository>();
        result.DiscoverRepository(repository.Info.WorkingDirectory);
        if (repository is Repository fixtureRepository)
        {
            fixtureRepository.Observe(result);
        }

        return result;
    }
}
