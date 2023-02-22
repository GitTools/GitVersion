using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion.Core.Tests;

public class GitVersionContextBuilder
{
    private IGitRepository? repository;
    private IReadOnlyDictionary<object, object?>? overrideConfiguration;
    public IServiceProvider? ServicesProvider;
    private Action<IServiceCollection>? overrideServices;

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
        var mockCommit = GitToolsTestingExtensions.CreateMockCommit();
        var mockBranch = GitToolsTestingExtensions.CreateMockBranch(branchName, mockCommit);

        this.repository ??= CreateRepository();

        var branches = this.repository.Branches.ToList();
        branches.Add(mockBranch);
        this.repository.Branches.GetEnumerator().Returns(_ => ((IEnumerable<IBranch>)branches).GetEnumerator());
        this.repository.Head.Returns(mockBranch);
        return this;
    }

    public void Build()
    {
        var repo = this.repository ?? CreateRepository();

        var options = Options.Create(new GitVersionOptions
        {
            WorkingDirectory = new EmptyRepositoryFixture().RepositoryPath,
            ConfigInfo = { OverrideConfiguration = this.overrideConfiguration }
        });

        this.ServicesProvider = ConfigureServices(services =>
        {
            services.AddSingleton(options);
            services.AddSingleton(repo);
            this.overrideServices?.Invoke(services);
        });
    }

    private static IGitRepository CreateRepository()
    {
        var mockCommit = GitToolsTestingExtensions.CreateMockCommit();
        var mockBranch = GitToolsTestingExtensions.CreateMockBranch(TestBase.MainBranch, mockCommit);
        var branches = Substitute.For<IBranchCollection>();
        branches.GetEnumerator().Returns(_ => ((IEnumerable<IBranch>)new[] { mockBranch }).GetEnumerator());

        var mockRepository = Substitute.For<IGitRepository>();
        mockRepository.Branches.Returns(branches);
        mockRepository.Head.Returns(mockBranch);
        mockRepository.Commits.Returns(mockBranch.Commits);

        return mockRepository;
    }

    private static IServiceProvider ConfigureServices(Action<IServiceCollection>? overrideServices = null)
    {
        var services = new ServiceCollection()
            .AddModule(new GitVersionCoreTestModule());

        overrideServices?.Invoke(services);

        return services.BuildServiceProvider();
    }
}
