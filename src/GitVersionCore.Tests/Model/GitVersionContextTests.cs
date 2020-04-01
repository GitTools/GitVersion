using System;
using System.Collections.Generic;
using GitTools.Testing;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using GitVersionCore.Tests.Helpers;
using GitVersionCore.Tests.Mocks;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
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

            var branchName = "master";
            var mockBranch = new MockBranch(branchName) { new MockCommit { CommitterEx = Generate.SignatureNow() } };
            var mockRepository = new MockRepository
            {
                Head = mockBranch,
                Branches = new MockBranchCollection
                {
                    mockBranch
                }
            };

            var context = GetGitVersionContext(mockRepository, branchName, config);

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

            var context = GetGitVersionContext(fixture.Repository, dummyBranchName, config);

            context.Configuration.Increment.ShouldBe(alternateExpected ?? increment);
        }

        [Test]
        public void UsesBranchSpecificConfigOverTopLevelDefaults()
        {
            var branchName = "develop";
            var config = new Config
            {
                VersioningMode = VersioningMode.ContinuousDelivery,
                Branches =
                {
                    {
                        branchName, new BranchConfig
                        {
                            VersioningMode = VersioningMode.ContinuousDeployment,
                            Tag = "alpha"
                        }
                    }
                }
            };
            config.Reset();
            var develop = new MockBranch(branchName) { new MockCommit { CommitterEx = Generate.SignatureNow() } };
            var mockRepository = new MockRepository
            {
                Head = develop,
                Branches = new MockBranchCollection
                {
                    new MockBranch("master") { new MockCommit { CommitterEx = Generate.SignatureNow() } },
                    develop
                }
            };

            var context = GetGitVersionContext(mockRepository, branchName, config);

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
                },
                Head = releaseLatestBranch
            };

            var latestContext = GetGitVersionContext(mockRepository, releaseLatestBranch.CanonicalName, config);
            latestContext.Configuration.Increment.ShouldBe(IncrementStrategy.None);

            mockRepository.Head = releaseVersionBranch;
            var versionContext = GetGitVersionContext(mockRepository, releaseVersionBranch.CanonicalName, config);
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

            var context = GetGitVersionContext(repo.Repository, "develop", config);

            context.Configuration.Increment.ShouldBe(IncrementStrategy.Major);
        }

        private static GitVersionContext GetGitVersionContext(IRepository repository, string branch, Config config = null)
        {
            var sp = BuildServiceProvider(repository, branch, config);
            return sp.GetService<Lazy<GitVersionContext>>().Value;
        }
    }
}
