using System;
using GitTools.Testing;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersionCore.Tests.Helpers;
using GitVersionCore.Tests.Mocks;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersionCore.Tests
{
    public class GitVersionContextBuilder
    {
        private IRepository repository;
        private Config configuration;
        public IServiceProvider ServicesProvider;
        private Action<IServiceCollection> overrideServices;

        public GitVersionContextBuilder WithRepository(IRepository gitRepository)
        {
            repository = gitRepository;
            return this;
        }

        public GitVersionContextBuilder WithConfig(Config config)
        {
            this.configuration = config;
            return this;
        }

        public GitVersionContextBuilder OverrideServices(Action<IServiceCollection> overrideServices = null)
        {
            this.overrideServices = overrideServices;
            return this;
        }

        public GitVersionContextBuilder WithTaggedMaster()
        {
            repository = CreateRepository();
            var target = repository.Head.Tip;
            ((MockTagCollection)repository.Tags).Add(new MockTag("1.0.0", target));
            return this;
        }

        public GitVersionContextBuilder AddCommit()
        {
            ((MockBranch)repository.Head).Add(new MockCommit());
            return this;
        }

        public GitVersionContextBuilder WithDevelopBranch()
        {
            return WithBranch("develop");
        }

        private GitVersionContextBuilder WithBranch(string branchName)
        {
            repository = CreateRepository();
            return AddBranch(branchName);
        }

        private GitVersionContextBuilder AddBranch(string branchName)
        {
            var mockBranch = new MockBranch(branchName)
            {
                new MockCommit()
            };
            ((MockBranchCollection)repository.Branches).Add(mockBranch);
            ((MockRepository)repository).Head = mockBranch;
            return this;
        }

        public GitVersionContext Build()
        {
            var repo = repository ?? CreateRepository();
            var config = configuration ?? new Config();

            config.Reset();

            var options = Options.Create(new Arguments { OverrideConfig = config });

            ServicesProvider = ConfigureServices(services =>
            {
                services.AddSingleton(options);
                overrideServices?.Invoke(services);
            });

            var gitVersionContextFactory = ServicesProvider.GetService<IGitVersionContextFactory>();

            gitVersionContextFactory.Init(repo, repo.Head);
            return gitVersionContextFactory.Context;
        }

        private static IRepository CreateRepository()
        {
            var mockBranch = new MockBranch("master") { new MockCommit { CommitterEx = Generate.SignatureNow() } };
            var mockRepository = new MockRepository
            {
                Branches = new MockBranchCollection
                {
                    mockBranch
                },
                Tags = new MockTagCollection(),
                Head = mockBranch
            };

            return mockRepository;
        }

        private static IServiceProvider ConfigureServices(Action<IServiceCollection> overrideServices = null)
        {
            var services = new ServiceCollection()
                .AddModule(new GitVersionCoreTestModule());

            overrideServices?.Invoke(services);

            return services.BuildServiceProvider();
        }
    }
}
