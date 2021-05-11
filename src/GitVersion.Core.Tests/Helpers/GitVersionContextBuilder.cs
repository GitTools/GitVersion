using System;
using System.Collections.Generic;
using System.Linq;
using GitTools.Testing;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GitVersion.Core.Tests
{
    public class GitVersionContextBuilder
    {
        private IGitRepository repository;
        private Config configuration;
        public IServiceProvider ServicesProvider;
        private Action<IServiceCollection> overrideServices;

        public GitVersionContextBuilder WithRepository(IGitRepository gitRepository)
        {
            repository = gitRepository;
            return this;
        }

        public GitVersionContextBuilder WithConfig(Config config)
        {
            configuration = config;
            return this;
        }

        public GitVersionContextBuilder OverrideServices(Action<IServiceCollection> overrides = null)
        {
            overrideServices = overrides;
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
            var mockCommit = GitToolsTestingExtensions.CreateMockCommit();
            var mockBranch = GitToolsTestingExtensions.CreateMockBranch(branchName, mockCommit);

            var branches = repository.Branches.ToList();
            branches.Add(mockBranch);
            repository.Branches.GetEnumerator().Returns(_ => ((IEnumerable<IBranch>)branches).GetEnumerator());
            repository.Head.Returns(mockBranch);
            return this;
        }

        public void Build()
        {
            var repo = repository ?? CreateRepository();

            var config = new ConfigurationBuilder()
                         .Add(configuration ?? new Config())
                         .Build();

            var options = Options.Create(new GitVersionOptions
            {
                WorkingDirectory = new EmptyRepositoryFixture().RepositoryPath,
                ConfigInfo = { OverrideConfig = config }
            });

            ServicesProvider = ConfigureServices(services =>
            {
                services.AddSingleton(options);
                services.AddSingleton(repo);
                overrideServices?.Invoke(services);
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

        private static IServiceProvider ConfigureServices(Action<IServiceCollection> overrideServices = null)
        {
            var services = new ServiceCollection()
                .AddModule(new GitVersionCoreTestModule());

            overrideServices?.Invoke(services);

            return services.BuildServiceProvider();
        }
    }
}
