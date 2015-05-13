using System;
using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class RemoteRepositoryScenarios
{
    [Test]
    public void GivenARemoteGitRepositoryWithCommits_ThenClonedLocalShouldMatchRemoteVersion()
    {
        using (var fixture = new RemoteRepositoryFixture(new Config()))
        {
            fixture.AssertFullSemver("0.1.0+4");
            fixture.AssertFullSemver("0.1.0+4", fixture.LocalRepository);
        }
    }

    [Test]
    public void GivenARemoteGitRepositoryAheadOfLocalRepository_ThenChangesShouldPull()
    {
        using (var fixture = new RemoteRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("0.1.0+5");
            fixture.AssertFullSemver("0.1.0+4", fixture.LocalRepository);
            fixture.LocalRepository.Network.Pull(fixture.LocalRepository.Config.BuildSignature(new DateTimeOffset(DateTime.Now)), new PullOptions());
            fixture.AssertFullSemver("0.1.0+5", fixture.LocalRepository);
        }
    }

    [Test]
    public void GivenARemoteGitRepositoryWhenCheckingOutDetachedhead_UsingExistingImplemenationThrowsException()
    {

        using (var fixture = new RemoteRepositoryFixture(new Config()))
        {
            fixture.IsForTrackedBranchOnly = false;
            fixture.LocalRepository.Checkout(fixture.LocalRepository.Head.Tip);

            Assert.Throws<WarningException>(() => fixture.AssertFullSemver("0.1.0+4", fixture.LocalRepository), "It looks like the branch being examined is a detached Head pointing to commit '{0}'. Without a proper branch name GitVersion cannot determine the build version.", fixture.LocalRepository.Head.Tip.Id.ToString(7));
        }
    }

    [Test]
    public void GivenARemoteGitRepositoryWhenCheckingOutDetachedhead_UsingTrackingBranchOnlyBehaviourShouldReturnVersion_0_1_4plus5()
    {

        using (var fixture = new RemoteRepositoryFixture(new Config()))
        {

            fixture.LocalRepository.Checkout(fixture.LocalRepository.Head.Tip);

            fixture.AssertFullSemver("0.1.0+4", fixture.LocalRepository);
        }
    }

    [Test]
    public void WhenLocalReleaseBranchPushedToTrackedMetaShouldNotChange()
    {
        var config = new Config()
        {
            VersioningMode = VersioningMode.ContinuousDeployment
        };

        using (var fixture = new RemoteRepositoryFixture(config))
        {
            const string TaggedVersion = "2.0";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            var remoteBranch = fixture.Repository.CreateBranch("release-3.0");
            fixture.LocalRepository.Network.Fetch(fixture.LocalRepository.Network.Remotes["origin"]);
            var localBranch = fixture.LocalRepository.CreateBranch("release-3.0", remoteBranch.Tip);

            fixture.LocalRepository.Branches.Update(localBranch, b =>
            {
                b.TrackedBranch = remoteBranch.CanonicalName;
            });

            var trackingBranch = fixture.LocalRepository.Branches["release-3.0"];
            fixture.LocalRepository.Checkout(trackingBranch);
            fixture.LocalRepository.MakeCommits(4);



            // Check version before push to remote. 
            fixture.AssertFullSemver("3.0.0-beta.4", fixture.LocalRepository);

            //TODO: This is not supported in LibGit2Sharp
            //fixture.LocalRepository.Network.Push(fixture.LocalRepository.Network.Remotes["origin"], @"refs/heads/release-3.0");

            // Check version after push to remote
            //fixture.AssertFullSemver("3.0.0-beta.4", fixture.LocalRepository);
        }
    }
}