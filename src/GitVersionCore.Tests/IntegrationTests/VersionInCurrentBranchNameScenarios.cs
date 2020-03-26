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
    public class VersionInCurrentBranchNameScenarios : TestBase
    {
        [Test]
        public void TakesVersionFromNameOfReleaseBranch()
        {
            using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
            fixture.BranchTo("release/2.0.0");

            fixture.AssertFullSemver("2.0.0-beta.1+0");
        }

        [Test]
        public void DoesNotTakeVersionFromNameOfNonReleaseBranch()
        {
            using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
            fixture.BranchTo("feature/upgrade-power-level-to-9000.0.1");

            fixture.AssertFullSemver("1.1.0-upgrade-power-level-to-9000-0-1.1+1");
        }

        [Test]
        public void TakesVersionFromNameOfBranchThatIsReleaseByConfig()
        {
            var config = new Config
            {
                Branches = new Dictionary<string, BranchConfig> { { "support", new BranchConfig { IsReleaseBranch = true } } }
            };

            using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
            fixture.BranchTo("support/2.0.0");

            fixture.AssertFullSemver("2.0.0+1", config);
        }

        [Test]
        public void TakesVersionFromNameOfRemoteReleaseBranchInOrigin()
        {
            using var fixture = new RemoteRepositoryFixture();
            fixture.BranchTo("release/2.0.0");
            fixture.MakeACommit();
            Commands.Fetch((Repository)fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, new string[0], new FetchOptions(), null);

            fixture.LocalRepositoryFixture.Checkout("origin/release/2.0.0");

            fixture.LocalRepositoryFixture.AssertFullSemver("2.0.0-beta.1+1");
        }

        [Test]
        public void DoesNotTakeVersionFromNameOfRemoteReleaseBranchInCustomRemote()
        {
            using var fixture = new RemoteRepositoryFixture();
            fixture.LocalRepositoryFixture.Repository.Network.Remotes.Rename("origin", "upstream");
            fixture.BranchTo("release/2.0.0");
            fixture.MakeACommit();
            Commands.Fetch((Repository)fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, new string[0], new FetchOptions(), null);

            fixture.LocalRepositoryFixture.Checkout("upstream/release/2.0.0");

            fixture.LocalRepositoryFixture.AssertFullSemver("0.1.0-beta.1+5");
        }
    }
}
