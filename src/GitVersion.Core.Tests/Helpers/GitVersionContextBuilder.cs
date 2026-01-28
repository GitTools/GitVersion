using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.Core.Tests;

public class GitVersionContextBuilder : IDisposable
{
    private IGitRepository? repository;
    private EmptyRepositoryFixture? emptyRepositoryFixture;
    private IReadOnlyDictionary<object, object?>? overrideConfiguration;
    private Action<IServiceCollection>? overrideServices;
    public IServiceProvider? ServicesProvider;

    public GitVersionContextBuilder WithRepository(IGitRepository gitRepository)
    {
        this.repository = gitRepository;
        return this;
    }

    public GitVersionContextBuilder WithOverrideConfiguration(IReadOnlyDictionary<object, object?>? value)
    {
        this.overrideConfiguration = value;
        return this;
    }

    public GitVersionContextBuilder OverrideServices(Action<IServiceCollection>? overrides = null)
    {
        this.overrideServices = overrides;
        return this;
    }

    public GitVersionContextBuilder WithDevelopBranch() => WithBranch("develop");

    private GitVersionContextBuilder WithBranch(string branchName)
    {
        this.repository = CreateRepository();
        return AddBranch(branchName);
    }

    private GitVersionContextBuilder AddBranch(string branchName)
    {
        var mockCommit = GitRepositoryTestingExtensions.CreateMockCommit();
        var mockBranch = GitRepositoryTestingExtensions.CreateMockBranch(branchName, mockCommit);

        this.repository ??= CreateRepository();

        var branches = this.repository.Branches.ToList();
        branches.Add(mockBranch);
        this.repository.Branches.MockCollectionReturn([.. branches]);
        this.repository.Head.Returns(mockBranch);
        return this;
    }

    public void Build()
    {
        var repo = this.repository ?? CreateRepository();

        emptyRepositoryFixture = new();
        var options = Options.Create(new GitVersionOptions { WorkingDirectory = emptyRepositoryFixture.RepositoryPath, ConfigurationInfo = { OverrideConfiguration = this.overrideConfiguration } });

        this.ServicesProvider = ConfigureServices(services =>
        {
            services.AddSingleton(options);
            services.AddSingleton(repo);
            this.overrideServices?.Invoke(services);
        });
    }

    private static IGitRepository CreateRepository()
    {
        var mockCommit = GitRepositoryTestingExtensions.CreateMockCommit();
        var mockBranch = GitRepositoryTestingExtensions.CreateMockBranch(TestBase.MainBranch, mockCommit);
        var branches = Substitute.For<IBranchCollection>();
        branches.MockCollectionReturn(mockBranch);

        var mockRepository = Substitute.For<IGitRepository>();
        mockRepository.Branches.Returns(branches);
        mockRepository.Head.Returns(mockBranch);
        mockRepository.Commits.Returns(mockBranch.Commits);

        return mockRepository;
    }

    private static ServiceProvider ConfigureServices(Action<IServiceCollection>? overrideServices = null)
    {
        var services = new ServiceCollection()
            .AddModule(new GitVersionCoreTestModule());

        overrideServices?.Invoke(services);

        return services.BuildServiceProvider();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        this.repository?.Dispose();
        this.emptyRepositoryFixture?.Dispose();
    }
}
