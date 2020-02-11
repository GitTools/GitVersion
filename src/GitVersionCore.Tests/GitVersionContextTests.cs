
using GitTools.Testing;
using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;
using GitVersion.Configuration;
using GitVersion.Logging;
using GitVersion.VersioningModes;
using GitVersionCore.Tests.Helpers;
using GitVersionCore.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersionCore.Tests
{
    public class GitVersionContextTests : TestBase
    {
        private ILog log;

        public GitVersionContextTests()
        {
            var sp = ConfigureServices();
            log = sp.GetService<ILog>();
        }

        [Test]
        [Theory]
        public void CanInheritVersioningMode(VersioningMode mode)
        {
            var config = new Config
            {
                VersioningMode = mode
            };
            config.Reset();

            var mockBranch = new MockBranch("master") { new MockCommit { CommitterEx = Generate.SignatureNow() } };
            var mockRepository = new MockRepository
            {
                Branches = new MockBranchCollection
                {
                    mockBranch
                }
            };

            var context = new GitVersionContext(mockRepository, log, mockBranch, config);
            context.Configuration.VersioningMode.ShouldBe(mode);
        }

        [TestCase(IncrementStrategy.Inherit, IncrementStrategy.Patch)] // Since it inherits, the increment strategy of master is used => Patch
        [TestCase(IncrementStrategy.Patch, null)]
        [TestCase(IncrementStrategy.Major, null)]
        [TestCase(IncrementStrategy.Minor, null)]
        [TestCase(IncrementStrategy.None, null)]
        public void CanInheritIncrement(IncrementStrategy increment, IncrementStrategy? alternateExpected)
        {
            // Dummy branch name to make sure that no default config exists.
            const string dummyBranchName = "dummy";

            var config = new Config
            {
                Increment = increment
            };
            config.Reset();

            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeACommit();
            fixture.BranchTo(dummyBranchName);
            fixture.MakeACommit();

            var context = new GitVersionContext(fixture.Repository, log, fixture.Repository.Branches[dummyBranchName], config);
            context.Configuration.Increment.ShouldBe(alternateExpected ?? increment);
        }

        [Test]
        public void UsesBranchSpecificConfigOverTopLevelDefaults()
        {
            var config = new Config
            {
                VersioningMode = VersioningMode.ContinuousDelivery,
                Branches =
                {
                    {
                        "develop", new BranchConfig
                        {
                            VersioningMode = VersioningMode.ContinuousDeployment,
                            Tag = "alpha"
                        }
                    }
                }
            };
            config.Reset();
            var develop = new MockBranch("develop") { new MockCommit { CommitterEx = Generate.SignatureNow() } };
            var mockRepository = new MockRepository
            {
                Branches = new MockBranchCollection
                {
                    new MockBranch("master") { new MockCommit { CommitterEx = Generate.SignatureNow() } },
                    develop
                }
            };
            var context = new GitVersionContext(mockRepository, log, develop, config);
            context.Configuration.Tag.ShouldBe("alpha");
        }

        [Test]
        public void UsesFirstBranchConfigWhenMultipleMatch()
        {
            var config = new Config
            {
                VersioningMode = VersioningMode.ContinuousDelivery,
                Branches =
                {
                    { "release/latest", new BranchConfig { Increment = IncrementStrategy.None, Regex = "release/latest", SourceBranches = new List<string>() } },
                    { "release", new BranchConfig { Increment = IncrementStrategy.Patch, Regex = "releases?[/-]", SourceBranches = new List<string>() } }
                }
            }.ApplyDefaults();

            var releaseLatestBranch = new MockBranch("release/latest") { new MockCommit { CommitterEx = Generate.SignatureNow() } };
            var releaseVersionBranch = new MockBranch("release/1.0.0") { new MockCommit { CommitterEx = Generate.SignatureNow() } };

            var mockRepository = new MockRepository
            {
                Branches = new MockBranchCollection
                {
                    releaseLatestBranch,
                    releaseVersionBranch
                }
            };

            var latestContext = new GitVersionContext(mockRepository, log, releaseLatestBranch, config);
            latestContext.Configuration.Increment.ShouldBe(IncrementStrategy.None);

            var versionContext = new GitVersionContext(mockRepository, log, releaseVersionBranch, config);
            versionContext.Configuration.Increment.ShouldBe(IncrementStrategy.Patch);
        }

        [Test]
        public void CanFindParentBranchForInheritingIncrementStrategy()
        {
            var config = new Config
            {
                Branches =
                {
                    { "develop", new BranchConfig { Increment = IncrementStrategy.Major} },
                    { "feature", new BranchConfig { Increment = IncrementStrategy.Inherit} }
                }
            }.ApplyDefaults();

            using var repo = new EmptyRepositoryFixture();
            repo.Repository.MakeACommit();
            Commands.Checkout(repo.Repository, repo.Repository.CreateBranch("develop"));
            repo.Repository.MakeACommit();
            var featureBranch = repo.Repository.CreateBranch("feature/foo");
            Commands.Checkout(repo.Repository, featureBranch);
            repo.Repository.MakeACommit();

            var context = new GitVersionContext(repo.Repository, log, repo.Repository.Head, config);
            context.Configuration.Increment.ShouldBe(IncrementStrategy.Major);
        }
    }
}
