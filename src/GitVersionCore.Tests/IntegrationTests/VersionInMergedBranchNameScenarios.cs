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

            fixture.AssertFullSemver("2.1.0-alpha.2"); // TODO : shouldn't commit count be 0 ?
        }

        [Test]
        public void TakesVersionFromNameOfReleaseBranchWhenMergedIntoMaster()
        {
            using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
            fixture.CreateAndMergeBranchIntoMaster("release/2.0.0");

            fixture.AssertFullSemver("2.0.0+0");
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

            fixture.AssertFullSemver("2.1.0-alpha.2", config); // TODO : shouldn't commit count be 0 ?
        }

        [Test]
        public void TakesVersionFromNameOfBranchThatIsReleaseByConfigWhenMergedIntoMaster()
        {
            var config = new Config
            {
                Branches = new Dictionary<string, BranchConfig> {
                    { "hotfix", new BranchConfig { IsReleaseBranch = true } },
                    // This will normally work because of increment rule in master, reconfigure for this test as we
                    // are testing versioning based on merged branch name.
                    { "master", new BranchConfig {  Increment = GitVersion.IncrementStrategy.None }  }
                }
            };

            using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
            fixture.CreateAndMergeBranchIntoMaster("hotfix/1.0.1");

            fixture.AssertFullSemver("1.0.1+0", config);
        }

        [Test]
        public void TakesVersionFromNameOfRemoteReleaseBranchInOrigin()
        {
            using var fixture = new RemoteRepositoryFixture();
            fixture.BranchTo("release/2.0.0");
            fixture.MakeACommit();
            Commands.Fetch((Repository)fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, new string[0], new FetchOptions(), null);
            // fixture.LocalRepositoryFixture.MergeNoFF("origin/release/2.0.0");
            // merge without default merge message, as that will invalidate these tests
            fixture.LocalRepositoryFixture.Repository.MergeNoFF("origin/release/2.0.0", "a merge message");

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

            // fixture.LocalRepositoryFixture.MergeNoFF("upstream/release/2.0.0");
            // merge without default merge message, as that will invalidate these tests
            fixture.LocalRepositoryFixture.Repository.MergeNoFF("upstream/release/2.0.0", "a merge message");

            fixture.LocalRepositoryFixture.AssertFullSemver("0.1.0+6");
        }
    }

    internal static class BaseGitFlowRepositoryFixtureExtensions
    {
        public static void CreateAndMergeBranchIntoDevelop(this BaseGitFlowRepositoryFixture fixture, string branchName)
            => CreateAndMergeBranch(fixture, branchName, "develop");

        public static void CreateAndMergeBranchIntoMaster(this BaseGitFlowRepositoryFixture fixture, string branchName)
            => CreateAndMergeBranch(fixture, branchName, "master");

        public static void CreateAndMergeBranch(this BaseGitFlowRepositoryFixture fixture, string sourceBranchName, string targetBranchName)
        {
            fixture.BranchTo(sourceBranchName);
            fixture.MakeACommit();
            fixture.Checkout(targetBranchName);
            // merge without default merge message, as that will invalidate these tests
            fixture.Repository.MergeNoFF(sourceBranchName, "a merge message");
        }
    }

    internal static class IRepositoryExtensions
    {
        public static void MergeNoFF(this IRepository repository, string branchName, string message)
        {
            repository.Merge(branchName, Generate.SignatureNow(), new MergeOptions { CommitOnSuccess = false, FastForwardStrategy = FastForwardStrategy.NoFastForward });
            repository.Commit(message, Generate.SignatureNow(), Generate.SignatureNow());
        }
    }
}
