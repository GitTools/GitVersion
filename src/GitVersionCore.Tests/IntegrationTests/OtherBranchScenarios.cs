using System.Collections.Generic;
using GitTools.Testing;
using GitVersion;
using GitVersion.Model.Configuration;
using GitVersionCore.Tests.Helpers;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests.IntegrationTests
{
    [TestFixture]
    public class OtherBranchScenarios : TestBase
    {
        [Test]
        public void CanTakeVersionFromReleaseBranch()
        {
            using var fixture = new EmptyRepositoryFixture();
            const string taggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(taggedVersion);
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("release/beta-2.0.0");
            Commands.Checkout(fixture.Repository, "release/beta-2.0.0");

            fixture.AssertFullSemver("2.0.0-beta.1+0");
        }

        [Test]
        public void BranchesWithIllegalCharsShouldNotBeUsedInVersionNames()
        {
            using var fixture = new EmptyRepositoryFixture();
            const string taggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(taggedVersion);
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("issue/m/github-569");
            Commands.Checkout(fixture.Repository, "issue/m/github-569");

            fixture.AssertFullSemver("1.0.4-issue-m-github-569.1+5");
        }

        [Test]
        public void ShouldNotGetVersionFromFeatureBranchIfNotMerged()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.0-unstable.0"); // initial commit in master

            fixture.Repository.CreateBranch("feature");
            Commands.Checkout(fixture.Repository, "feature");
            fixture.Repository.MakeATaggedCommit("1.0.1-feature.1");

            Commands.Checkout(fixture.Repository, "master");
            fixture.Repository.CreateBranch("develop");
            Commands.Checkout(fixture.Repository, "develop");
            fixture.Repository.MakeACommit();

            var version = fixture.GetVersion();
            version.SemVer.ShouldBe("1.0.0-alpha.1");
        }

        [TestCase("alpha", "JIRA-123", "alpha")]
        [TestCase("useBranchName", "JIRA-123", "JIRA-123")]
        [TestCase("alpha.{BranchName}", "JIRA-123", "alpha.JIRA-123")]
        public void ShouldUseConfiguredTagWhenNotTrimmingBranchName(string tag, string branchName, string preReleaseTagName)
        {
            var config = new Config
            {
                Branches =
                {
                    {
                        "other",
                        new BranchConfig
                        {
                            Increment = IncrementStrategy.Patch,
                            Regex = ".*",
                            TrimRegex = "",
                            SourceBranches = new HashSet<string>(),
                            Tag = tag
                        }
                    }
                }
            };

            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch(branchName);
            Commands.Checkout(fixture.Repository, branchName);
            fixture.Repository.MakeCommits(5);

            var expectedFullSemVer = $"1.0.1-{preReleaseTagName}.1+5";
            fixture.AssertFullSemver(expectedFullSemVer, config);
        }

        [TestCase("alpha", "JIRA-123", "alpha")]
        [TestCase("useBranchName", "JIRA-123", "JIRA-123")]
        [TestCase("alpha.{BranchName}", "JIRA-123", "alpha.JIRA-123")]
        public void ShouldUseConfiguredTagWithTrimmedBranchName(string tag, string name, string preReleaseTagName)
        {
            var config = new Config
            {
                Branches =
                {
                    {
                        "other",
                        new BranchConfig
                        {
                            Increment = IncrementStrategy.Patch,
                            Regex = ".*",
                            TrimRegex = "^something\\/weird(.*?)\\.and-more",
                            SourceBranches = new HashSet<string>(),
                            Tag = tag
                        }
                    }
                }
            };

            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            var branchName = $"something/weird{name}.and-more";
            fixture.Repository.CreateBranch(branchName);
            Commands.Checkout(fixture.Repository, branchName);
            fixture.Repository.MakeCommits(5);

            var expectedFullSemVer = $"1.0.1-{preReleaseTagName}.1+5";
            fixture.AssertFullSemver(expectedFullSemVer, config);
        }
    }
}
