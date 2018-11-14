namespace GitTools.Tests.Git
{
    using GitTools.Git;
    using LibGit2Sharp;
    using NUnit.Framework;
    using Shouldly;
    using Testing;

    public class GitRepositoryHelperTests
    {
        [Test]
        public void NormalisationOfPullRequestsWithFetch()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeACommit();

                Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("feature/foo"));
                fixture.Repository.MakeACommit();
                var commit = fixture.Repository.CreatePullRequestRef("feature/foo", "master", prNumber: 3);
                using (var localFixture = fixture.CloneRepository())
                {
                    localFixture.Checkout(commit.Sha);
                    GitRepositoryHelper.NormalizeGitDirectory(localFixture.RepositoryPath, new AuthenticationInfo(), noFetch: false, currentBranch: string.Empty);

                    var normalisedPullBranch = localFixture.Repository.Branches["pull/3/merge"];
                    normalisedPullBranch.ShouldNotBe(null);
                }
            }
        }

        [Test]
        public void NormalisationOfPullRequestsWithoutFetch()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeACommit();

                Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("feature/foo"));
                fixture.Repository.MakeACommit();
                var commit = fixture.Repository.CreatePullRequestRef("feature/foo", "master", prNumber: 3, allowFastFowardMerge: true);
                using (var localFixture = fixture.CloneRepository())
                {
                    localFixture.Checkout(commit.Sha);
                    GitRepositoryHelper.NormalizeGitDirectory(localFixture.RepositoryPath, new AuthenticationInfo(), noFetch: true, currentBranch: "refs/pull/3/merge");

                    var normalisedPullBranch = localFixture.Repository.FindBranch("pull/3/merge");
                    normalisedPullBranch.ShouldNotBe(null);
                }
            }
        }

        [Test]
        public void NormalisationOfTag()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeACommit();

                Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("feature/foo"));
                fixture.Repository.MakeACommit();

                fixture.BranchTo("release/2.0.0");
                fixture.MakeACommit();
                fixture.MakeATaggedCommit("2.0.0-rc.1");
                fixture.Checkout("master");
                fixture.MergeNoFF("release/2.0.0");
                fixture.Repository.Branches.Remove(fixture.Repository.Branches["release/2.0.0"]);
                var remoteTagSha = fixture.Repository.Tags["2.0.0-rc.1"].Target.Sha;

                using (var localFixture = fixture.CloneRepository())
                {
                    localFixture.Checkout(remoteTagSha);
                    GitRepositoryHelper.NormalizeGitDirectory(localFixture.RepositoryPath, new AuthenticationInfo(), noFetch: false, currentBranch: string.Empty);

                    localFixture.Repository.Head.FriendlyName.ShouldBe("(no branch)");
                    localFixture.Repository.Head.Tip.Sha.ShouldBe(remoteTagSha);
                }
            }
        }

        [Test]
        public void UpdatesCurrentBranch()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeACommit();
                Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
                fixture.Repository.MakeACommit();
                Commands.Checkout(fixture.Repository, "master");
                using (var localFixture = fixture.CloneRepository())
                {
                    // Advance remote
                    Commands.Checkout(fixture.Repository, "develop");
                    var advancedCommit = fixture.Repository.MakeACommit();
                    Commands.Fetch((Repository)localFixture.Repository, localFixture.Repository.Network.Remotes["origin"].Name, new string[0], null, null);
                    Commands.Checkout(localFixture.Repository, advancedCommit.Sha);
                    GitRepositoryHelper.NormalizeGitDirectory(localFixture.RepositoryPath, new AuthenticationInfo(), noFetch: false, currentBranch: "refs/heads/develop");

                    var normalisedBranch = localFixture.Repository.Branches["develop"];
                    normalisedBranch.ShouldNotBe(null);
                    normalisedBranch.Tip.Sha.ShouldBe(advancedCommit.Sha);
                    localFixture.Repository.Head.Tip.Sha.ShouldBe(advancedCommit.Sha);
                }
            }
        }

        [Test]
        public void ShouldNotChangeBranchWhenNormalizingTheDirectory()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeATaggedCommit("v1.0.0");

                Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("develop"));
                var lastCommitOnDevelop = fixture.Repository.MakeACommit();

                Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("feature/foo"));
                fixture.Repository.MakeACommit();

                using (var localFixture = fixture.CloneRepository())
                {
                    Commands.Checkout(localFixture.Repository, "origin/develop");

                    // Another commit on feature/foo will force an update
                    fixture.Checkout("feature/foo");
                    fixture.Repository.MakeACommit();

                    GitRepositoryHelper.NormalizeGitDirectory(localFixture.RepositoryPath, new AuthenticationInfo(), noFetch: false, currentBranch: null);

                    localFixture.Repository.Head.Tip.Sha.ShouldBe(lastCommitOnDevelop.Sha);
                }
            }
        }

        [Test]
        public void ShouldNotMoveLocalBranchWhenRemoteAdvances()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeACommit();

                Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("feature/foo"));
                fixture.Repository.MakeACommit();
                using (var localFixture = fixture.CloneRepository())
                {
                    localFixture.Checkout("feature/foo");
                    var expectedTip = localFixture.Repository.Head.Tip;
                    // Advance remote
                    fixture.Repository.MakeACommit();
                    GitRepositoryHelper.NormalizeGitDirectory(localFixture.RepositoryPath, new AuthenticationInfo(), noFetch: false, currentBranch: null);

                    var normalisedBranch = localFixture.Repository.Branches["feature/foo"];
                    normalisedBranch.ShouldNotBe(null);
                    normalisedBranch.Tip.Sha.ShouldBe(expectedTip.Sha);
                }
            }
        }

        [Test]
        public void CheckedOutShaShouldNotChanged()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeACommit();
                var commitToBuild = fixture.Repository.MakeACommit();
                fixture.Repository.MakeACommit();

                using (var localFixture = fixture.CloneRepository())
                {
                    Commands.Checkout(localFixture.Repository, commitToBuild);
                    GitRepositoryHelper.NormalizeGitDirectory(localFixture.RepositoryPath, new AuthenticationInfo(), noFetch: false, currentBranch: "refs/heads/master");

                    var normalisedBranch = localFixture.Repository.Branches["master"];
                    normalisedBranch.Tip.Sha.ShouldBe(commitToBuild.Sha);
                }
            }
        }


        [Test]
        // Copied from GitVersion, to attempt fixing this bug: https://travis-ci.org/GitTools/GitVersion/jobs/171288284#L2025
        public void GitHubFlowMajorRelease()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.SequenceDiagram.Participant("master");

                fixture.Repository.MakeACommit();
                fixture.ApplyTag("1.3.0");

                // Create release branch
                fixture.BranchTo("release/2.0.0", "release");
                fixture.SequenceDiagram.Activate("release/2.0.0");
                fixture.MakeACommit();
                // fixture.AssertFullSemver("2.0.0-beta.1+1");
                fixture.MakeACommit();
                // fixture.AssertFullSemver("2.0.0-beta.1+2");

                // Apply beta.1 tag should be exact tag
                fixture.ApplyTag("2.0.0-beta.1");
                // fixture.AssertFullSemver("2.0.0-beta.1");

                // test that the CommitsSinceVersionSource should still return commit count
                // var version = fixture.GetVersion();
                // version.CommitsSinceVersionSource.ShouldBe("2");

                // Make a commit after a tag should bump up the beta
                fixture.MakeACommit();
                // fixture.AssertFullSemver("2.0.0-beta.2+3");

                // Complete release
                fixture.Checkout("master");
                fixture.MergeNoFF("release/2.0.0");
                fixture.SequenceDiagram.Destroy("release/2.0.0");
                fixture.SequenceDiagram.NoteOver("Release branches are deleted once merged", "release/2.0.0");

                //fixture.AssertFullSemver("2.0.0+0");
                fixture.ApplyTag("2.0.0");
                // fixture.AssertFullSemver("2.0.0");
                fixture.MakeACommit();

#if !NETCOREAPP1_1
                fixture.Repository.DumpGraph();
#endif
                // fixture.AssertFullSemver("2.0.1+1");
            }
        }
    }
}