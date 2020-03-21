
using System.Collections.Generic;
using GitTools.Testing;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.VersionCalculation;
using GitVersionCore.Tests.Helpers;
using GitVersionCore.Tests.Mocks;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests
{
    public class GitVersionContextTests : TestBase
    {
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

            var gitVersionContextFactory = GetGitVersionContextFactory(config);
            gitVersionContextFactory.Init(mockRepository, mockBranch);
            var context = gitVersionContextFactory.Context;

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

            var gitVersionContextFactory = GetGitVersionContextFactory(config);
            gitVersionContextFactory.Init(fixture.Repository, fixture.Repository.Branches[dummyBranchName]);
            var context = gitVersionContextFactory.Context;

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

            var gitVersionContextFactory = GetGitVersionContextFactory(config);
            gitVersionContextFactory.Init(mockRepository, develop);
            var context = gitVersionContextFactory.Context;

            context.Configuration.Tag.ShouldBe("alpha");
        }

        [Test]
        public void UsesFirstBranchConfigWhenMultipleMatch()
        {
            var branchConfig = new BranchConfig
            {
                VersioningMode = VersioningMode.Mainline,
                Increment = IncrementStrategy.None,
                PreventIncrementOfMergedBranchVersion = false,
                TrackMergeTarget = false,
                TracksReleaseBranches = false,
                IsReleaseBranch = false,
                SourceBranches = new List<string>()
            };
            var config = new Config
            {
                VersioningMode = VersioningMode.ContinuousDelivery,
                Branches =
                {
                    { "release/latest", new BranchConfig(branchConfig) { Increment = IncrementStrategy.None, Regex = "release/latest" } },
                    { "release", new BranchConfig(branchConfig) { Increment = IncrementStrategy.Patch, Regex = "releases?[/-]" } }
                }
            };

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

            var gitVersionContextFactory = GetGitVersionContextFactory(config);
            gitVersionContextFactory.Init(mockRepository, releaseLatestBranch);
            var latestContext = gitVersionContextFactory.Context;

            latestContext.Configuration.Increment.ShouldBe(IncrementStrategy.None);

            gitVersionContextFactory.Init(mockRepository, releaseVersionBranch);
            var versionContext = gitVersionContextFactory.Context;
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

            var gitVersionContextFactory = GetGitVersionContextFactory(config);
            gitVersionContextFactory.Init(repo.Repository, repo.Repository.Head);
            var context = gitVersionContextFactory.Context;

            context.Configuration.Increment.ShouldBe(IncrementStrategy.Major);
        }

        private static IGitVersionContextFactory GetGitVersionContextFactory(Config config = null)
        {
            config ??= new Config().ApplyDefaults();
            var options = Options.Create(new Arguments { OverrideConfig = config });

            var sp = ConfigureServices(services => { services.AddSingleton(options); });

            return sp.GetService<IGitVersionContextFactory>();
        }
    }
}
