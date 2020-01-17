using System.Collections.Generic;
using System.Linq;
using GitTools.Testing;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.Logging;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.BaseVersionCalculators;
using GitVersionCore.Tests.Helpers;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests.VersionCalculation.Strategies
{
    [TestFixture]
    public class VersionInBranchNameBaseVersionStrategyTests : TestBase
    {
        private readonly ILog log;
        private readonly IVersionStrategy strategy;
        private readonly Config configuration;

        public VersionInBranchNameBaseVersionStrategyTests()
        {
            log = new NullLog();
            strategy = new VersionInBranchNameVersionStrategy();
            configuration = new Config().ApplyDefaults();
        }

        [TestCase("release-2.0.0", "2.0.0")]
        [TestCase("release/3.0.0", "3.0.0")]
        public void CanTakeVersionFromNameOfReleaseBranch(string branchName, string expectedBaseVersion)
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();
            var branch = fixture.Repository.CreateBranch(branchName);

            var gitVersionContext = new GitVersionContext(fixture.Repository, log, branch, configuration);
            var baseVersion = strategy.GetVersions(gitVersionContext).Single();

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

            var gitVersionContext = new GitVersionContext(fixture.Repository, log, branch, configuration);
            var baseVersions = strategy.GetVersions(gitVersionContext);

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
            configuration.Branches = branchConfigs;

            var gitVersionContext = new GitVersionContext(fixture.Repository, log, branch, configuration);
            var baseVersion = strategy.GetVersions(gitVersionContext).Single();

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

            var gitVersionContext = new GitVersionContext(fixture.Repository, log, branch, configuration);
            var baseVersion = strategy.GetVersions(gitVersionContext).Single();

            baseVersion.SemanticVersion.ToString().ShouldBe(expectedBaseVersion);
        }
    }
}
