using System;
using System.Collections.Generic;
using System.Linq;
using GitTools.Testing;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using GitVersionCore.Tests.Helpers;
using GitVersionCore.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GitVersionCore.Tests
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

            ((MockRepository)repository).Head = mockBranch;
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
            var mockBranch = GitToolsTestingExtensions.CreateMockBranch("master", mockCommit);
            var branches = Substitute.For<IBranchCollection>();
            branches.GetEnumerator().Returns(_ => ((IEnumerable<IBranch>)new[] { mockBranch }).GetEnumerator());
            var mockRepository = new MockRepository
            {
                Branches = branches,
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
