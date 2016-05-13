using System;
using System.ComponentModel;
using GitTools.Git;
using GitTools.Testing;
using GitVersionCore.Tests;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class RemoteRepositoryScenarios
{
    [Test]
    public void GivenARemoteGitRepositoryWithCommits_ThenClonedLocalShouldMatchRemoteVersion()
    {
        using (var fixture = new RemoteRepositoryFixture())
        {
            fixture.AssertFullSemver("0.1.0+4");
            fixture.AssertFullSemver("0.1.0+4", fixture.LocalRepositoryFixture.Repository);
        }
    }

    [Test]
    public void GivenARemoteGitRepositoryWithCommitsAndBranches_ThenClonedLocalShouldMatchRemoteVersion()
    {
        using (var fixture = new RemoteRepositoryFixture(
            path =>
            {
                Repository.Init(path);
                Console.WriteLine("Created git repository at '{0}'", path);

                var repo = new Repository(path);
                repo.MakeCommits(5);

                repo.CreateBranch("develop");
                repo.CreateBranch("release-1.0");

                repo.Checkout("release-1.0");
                repo.MakeCommits(5);

                return repo;
            }))
        {
            GitRepositoryHelper.NormalizeGitDirectory(fixture.LocalRepositoryFixture.RepositoryPath, new AuthenticationInfo(), noFetch: false, currentBranch: string.Empty);

            fixture.AssertFullSemver("1.0.0-beta.1+5");
            fixture.AssertFullSemver("1.0.0-beta.1+5", fixture.LocalRepositoryFixture.Repository);
        }
    }

    [Test]
    public void GivenARemoteGitRepositoryAheadOfLocalRepository_ThenChangesShouldPull()
    {
        using (var fixture = new RemoteRepositoryFixture())
        {
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("0.1.0+5");
            fixture.AssertFullSemver("0.1.0+4", fixture.LocalRepositoryFixture.Repository);
            var buildSignature = fixture.LocalRepositoryFixture.Repository.Config.BuildSignature(new DateTimeOffset(DateTime.Now));
            Commands.Pull((Repository) fixture.LocalRepositoryFixture.Repository, buildSignature, new PullOptions());
            fixture.AssertFullSemver("0.1.0+5", fixture.LocalRepositoryFixture.Repository);
        }
    }

    [Test]
    public void GivenARemoteGitRepositoryWhenCheckingOutDetachedhead_UsingExistingImplementationThrowsException()
    {

        using (var fixture = new RemoteRepositoryFixture())
        {
            fixture.LocalRepositoryFixture.Repository.Checkout(fixture.LocalRepositoryFixture.Repository.Head.Tip);

            Should.Throw<WarningException>(() => fixture.AssertFullSemver("0.1.0+4", fixture.LocalRepositoryFixture.Repository, isForTrackedBranchOnly: false),
                string.Format("It looks like the branch being examined is a detached Head pointing to commit '{0}'. Without a proper branch name GitVersion cannot determine the build version.",
                fixture.LocalRepositoryFixture.Repository.Head.Tip.Id.ToString(7)));
        }
    }

    [Test]
    public void GivenARemoteGitRepositoryWhenCheckingOutDetachedhead_UsingTrackingBranchOnlyBehaviourShouldReturnVersion_0_1_4plus5()
    {
        using (var fixture = new RemoteRepositoryFixture())
        {
            fixture.LocalRepositoryFixture.Repository.Checkout(fixture.LocalRepositoryFixture.Repository.Head.Tip);

            fixture.AssertFullSemver("0.1.0+4", fixture.LocalRepositoryFixture.Repository);
        }
    }
}