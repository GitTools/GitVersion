using System;
using GitTools.Testing;
using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;
using GitVersion.Exceptions;
using GitVersion.Helpers;
using GitVersion.Logging;

namespace GitVersionCore.Tests.IntegrationTests
{
    [TestFixture]
    public class RemoteRepositoryScenarios : TestBase
    {
        [Test]
        public void GivenARemoteGitRepositoryWithCommitsThenClonedLocalShouldMatchRemoteVersion()
        {
            using var fixture = new RemoteRepositoryFixture();
            fixture.AssertFullSemver("0.1.0+4");
            fixture.AssertFullSemver("0.1.0+4", fixture.LocalRepositoryFixture.Repository);
        }

        [Test]
        public void GivenARemoteGitRepositoryWithCommitsAndBranchesThenClonedLocalShouldMatchRemoteVersion()
        {
            using var fixture = new RemoteRepositoryFixture(
                path =>
                {
                    Repository.Init(path);
                    Console.WriteLine("Created git repository at '{0}'", path);

                    var repo = new Repository(path);
                    repo.MakeCommits(5);

                    repo.CreateBranch("develop");
                    repo.CreateBranch("release-1.0");

                    Commands.Checkout(repo, "release-1.0");
                    repo.MakeCommits(5);

                    return repo;
                });
            GitRepositoryHelper.NormalizeGitDirectory(new NullLog(), new TestEnvironment(), fixture.LocalRepositoryFixture.RepositoryPath, new AuthenticationInfo(), noFetch: false, currentBranch: string.Empty, isDynamicRepository: true);

            fixture.AssertFullSemver("1.0.0-beta.1+5");
            fixture.AssertFullSemver("1.0.0-beta.1+5", fixture.LocalRepositoryFixture.Repository);
        }

        [Test]
        public void GivenARemoteGitRepositoryAheadOfLocalRepositoryThenChangesShouldPull()
        {
            using var fixture = new RemoteRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("0.1.0+5");
            fixture.AssertFullSemver("0.1.0+4", fixture.LocalRepositoryFixture.Repository);
            var buildSignature = fixture.LocalRepositoryFixture.Repository.Config.BuildSignature(new DateTimeOffset(DateTime.Now));
            Commands.Pull((Repository) fixture.LocalRepositoryFixture.Repository, buildSignature, new PullOptions());
            fixture.AssertFullSemver("0.1.0+5", fixture.LocalRepositoryFixture.Repository);
        }

        [Test]
        public void GivenARemoteGitRepositoryWhenCheckingOutDetachedheadUsingExistingImplementationThrowsException()
        {
            using var fixture = new RemoteRepositoryFixture();
            Commands.Checkout(
                fixture.LocalRepositoryFixture.Repository,
                fixture.LocalRepositoryFixture.Repository.Head.Tip);

            Should.Throw<WarningException>(() => fixture.AssertFullSemver("0.1.0+4", fixture.LocalRepositoryFixture.Repository, isForTrackedBranchOnly: false),
                $"It looks like the branch being examined is a detached Head pointing to commit '{fixture.LocalRepositoryFixture.Repository.Head.Tip.Id.ToString(7)}'. Without a proper branch name GitVersion cannot determine the build version.");
        }

        [Test]
        public void GivenARemoteGitRepositoryWhenCheckingOutDetachedheadUsingTrackingBranchOnlyBehaviourShouldReturnVersion014Plus5()
        {
            using var fixture = new RemoteRepositoryFixture();
            Commands.Checkout(fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Head.Tip);

            fixture.AssertFullSemver("0.1.0+4", fixture.LocalRepositoryFixture.Repository);
        }
    }
}
