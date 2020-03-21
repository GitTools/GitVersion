using System.Collections.Generic;
using System.Linq;
using GitTools.Testing;
using GitVersion;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.VersionCalculation;
using GitVersionCore.Tests.Helpers;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests.VersionCalculation.Strategies
{
    [TestFixture]
    public class VersionInBranchNameBaseVersionStrategyTests : TestBase
    {
        private IVersionStrategy strategy;

        [TestCase("release-2.0.0", "2.0.0")]
        [TestCase("release/3.0.0", "3.0.0")]
        public void CanTakeVersionFromNameOfReleaseBranch(string branchName, string expectedBaseVersion)
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();
            var branch = fixture.Repository.CreateBranch(branchName);

            var gitVersionContextFactory = GetGitVersionContextFactory();
            gitVersionContextFactory.Init(fixture.Repository, branch);
            var context = gitVersionContextFactory.Context;
            var baseVersion = strategy.GetVersions(context).Single();

            baseVersion.SemanticVersion.ToString().ShouldBe(expectedBaseVersion);
        }

        [TestCase("hotfix-2.0.0")]
        [TestCase("hotfix/2.0.0")]
        [TestCase("custom/JIRA-123")]
        [TestCase("hotfix/downgrade-to-gitversion-3.6.5-to-fix-miscalculated-version")]
        public void ShouldNotTakeVersionFromNameOfNonReleaseBranch(string branchName)
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();
            var branch = fixture.Repository.CreateBranch(branchName);

            var gitVersionContextFactory = GetGitVersionContextFactory();
            gitVersionContextFactory.Init(fixture.Repository, branch);
            var context = gitVersionContextFactory.Context;
            var baseVersions = strategy.GetVersions(context);

            baseVersions.ShouldBeEmpty();
        }

        [TestCase("support/lts-2.0.0", "2.0.0")]
        [TestCase("support-3.0.0-lts", "3.0.0")]
        public void CanTakeVersionFromNameOfConfiguredReleaseBranch(string branchName, string expectedBaseVersion)
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();
            var branch = fixture.Repository.CreateBranch(branchName);
            var branchConfigs = new Dictionary<string, BranchConfig> { { "support", new BranchConfig { IsReleaseBranch = true } } };

            var config = new Config().ApplyDefaults();
            config.Branches = branchConfigs;
            var gitVersionContextFactory = GetGitVersionContextFactory(config);

            gitVersionContextFactory.Init(fixture.Repository, branch);
            var context = gitVersionContextFactory.Context;
            var baseVersion = strategy.GetVersions(context).Single();

            baseVersion.SemanticVersion.ToString().ShouldBe(expectedBaseVersion);
        }

        [TestCase("release-2.0.0", "2.0.0")]
        [TestCase("release/3.0.0", "3.0.0")]
        public void CanTakeVersionFromNameOfRemoteReleaseBranch(string branchName, string expectedBaseVersion)
        {
            using var fixture = new RemoteRepositoryFixture();
            var branch = fixture.Repository.CreateBranch(branchName);
            Commands.Checkout(fixture.Repository, branch);
            fixture.MakeACommit();

            Commands.Fetch((Repository)fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, new string[0], new FetchOptions(), null);
            fixture.LocalRepositoryFixture.Checkout($"origin/{branchName}");

            var gitVersionContextFactory = GetGitVersionContextFactory();
            gitVersionContextFactory.Init(fixture.Repository, branch);
            var context = gitVersionContextFactory.Context;
            var baseVersion = strategy.GetVersions(context).Single();

            baseVersion.SemanticVersion.ToString().ShouldBe(expectedBaseVersion);
        }

        private IGitVersionContextFactory GetGitVersionContextFactory(Config config = null)
        {
            config ??= new Config().ApplyDefaults();
            var options = Options.Create(new Arguments { OverrideConfig = config });

            var sp = ConfigureServices(services => { services.AddSingleton(options); });
            var gitRepoMetadataProvider = sp.GetService<IGitRepoMetadataProvider>();
            var gitVersionContextFactory = sp.GetService<IGitVersionContextFactory>();
            strategy = new VersionInBranchNameVersionStrategy(gitRepoMetadataProvider, gitVersionContextFactory);
            return sp.GetService<IGitVersionContextFactory>();
        }
    }
}
