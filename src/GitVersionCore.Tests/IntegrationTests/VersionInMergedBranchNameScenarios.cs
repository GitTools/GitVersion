using System.Collections.Generic;
using System.Linq;
using GitTools.Testing;
using GitVersion.Model.Configuration;
using GitVersionCore.Tests.Helpers;
using LibGit2Sharp;
using NUnit.Framework;

namespace GitVersionCore.Tests.IntegrationTests
{
    [TestFixture]
    public class VersionInMergedBranchNameScenarios : TestBase
    {
        [Test]
        public void TakesVersionFromNameOfReleaseBranch()
        {
            using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
            fixture.CreateAndMergeBranchIntoDevelop("release/2.0.0");

            fixture.AssertFullSemver("2.1.0-alpha.2");
        }

        [Test]
        public void DoesNotTakeVersionFromNameOfNonReleaseBranch()
        {
            using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
            fixture.CreateAndMergeBranchIntoDevelop("pull-request/improved-by-upgrading-some-lib-to-4.5.6");
            fixture.CreateAndMergeBranchIntoDevelop("hotfix/downgrade-some-lib-to-3.2.1-to-avoid-breaking-changes");

            fixture.AssertFullSemver("1.1.0-alpha.5");
        }

        [Test]
        public void TakesVersionFromNameOfBranchThatIsReleaseByConfig()
        {
            var config = new Config
            {
                Branches = new Dictionary<string, BranchConfig> { { "support", new BranchConfig { IsReleaseBranch = true } } }
            };

            using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
            fixture.CreateAndMergeBranchIntoDevelop("support/2.0.0");

            fixture.AssertFullSemver("2.1.0-alpha.2", config);
        }

        [Test]
        public void TakesVersionFromNameOfRemoteReleaseBranchInOrigin()
        {
            using var fixture = new RemoteRepositoryFixture();
            fixture.BranchTo("release/2.0.0");
            fixture.MakeACommit();
            Commands.Fetch((Repository)fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, new string[0], new FetchOptions(), null);

            fixture.LocalRepositoryFixture.MergeNoFF("origin/release/2.0.0");

            fixture.LocalRepositoryFixture.AssertFullSemver("2.0.0+0");
        }

        [Test]
        public void DoesNotTakeVersionFromNameOfRemoteReleaseBranchInCustomRemote()
        {
            using var fixture = new RemoteRepositoryFixture();
            fixture.LocalRepositoryFixture.Repository.Network.Remotes.Rename("origin", "upstream");
            fixture.BranchTo("release/2.0.0");
            fixture.MakeACommit();
            Commands.Fetch((Repository)fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, new string[0], new FetchOptions(), null);

            fixture.LocalRepositoryFixture.MergeNoFF("upstream/release/2.0.0");

            fixture.LocalRepositoryFixture.AssertFullSemver("0.1.0+6");
        }
    }

    internal static class BaseGitFlowRepositoryFixtureExtensions
    {
        public static void CreateAndMergeBranchIntoDevelop(this BaseGitFlowRepositoryFixture fixture, string branchName)
        {
            fixture.BranchTo(branchName);
            fixture.MakeACommit();
            fixture.Checkout("develop");
            fixture.MergeNoFF(branchName);
        }
    }
}
