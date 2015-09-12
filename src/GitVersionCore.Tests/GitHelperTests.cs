namespace GitVersionCore.Tests
{
    using GitTools.Testing;
    using GitTools.Testing.Fixtures;
    using GitVersion;
    using LibGit2Sharp;
    using NUnit.Framework;
    using Shouldly;

    public class GitHelperTests
    {
        [Test]
        public void NormalisationOfPullRequestsWithFetch()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeACommit();

                fixture.Repository.Checkout(fixture.Repository.CreateBranch("feature/foo"));
                fixture.Repository.MakeACommit();
                var commit = fixture.Repository.CreatePullRequestRef("feature/foo", "master", prNumber: 3);
                using (var localFixture = fixture.CloneRepository())
                {
                    localFixture.Checkout(commit.Sha);
                    GitHelper.NormalizeGitDirectory(localFixture.RepositoryPath, new Authentication(), noFetch: false, currentBranch: string.Empty);

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

                fixture.Repository.Checkout(fixture.Repository.CreateBranch("feature/foo"));
                fixture.Repository.MakeACommit();
                var commit = fixture.Repository.CreatePullRequestRef("feature/foo", "master", prNumber: 3, allowFastFowardMerge: true);
                using (var localFixture = fixture.CloneRepository())
                {
                    localFixture.Checkout(commit.Sha);
                    GitHelper.NormalizeGitDirectory(localFixture.RepositoryPath, new Authentication(), noFetch: true, currentBranch: "refs/pull/3/merge");

                    var normalisedPullBranch = localFixture.Repository.Branches["pull/3/merge"];
                    normalisedPullBranch.ShouldNotBe(null);
                }
            }
        }

        [Test]
        public void UpdatesLocalBranchesWhen()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeACommit();

                fixture.Repository.Checkout(fixture.Repository.CreateBranch("feature/foo"));
                fixture.Repository.MakeACommit();
                using (var localFixture = fixture.CloneRepository())
                {
                    localFixture.Checkout("feature/foo");
                    // Advance remote
                    var advancedCommit = fixture.Repository.MakeACommit();
                    GitHelper.NormalizeGitDirectory(localFixture.RepositoryPath, new Authentication(), noFetch: false, currentBranch: null);

                    var normalisedBranch = localFixture.Repository.Branches["feature/foo"];
                    normalisedBranch.ShouldNotBe(null);
                    normalisedBranch.Tip.Sha.ShouldBe(advancedCommit.Sha);
                }
            }
        }

        [Test]
        public void UpdatesCurrentBranch()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeACommit();
                fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
                fixture.Repository.MakeACommit();
                fixture.Repository.Checkout("master");
                using (var localFixture = fixture.CloneRepository())
                {
                    // Advance remote
                    fixture.Repository.Checkout("develop");
                    var advancedCommit = fixture.Repository.MakeACommit();
                    localFixture.Repository.Network.Fetch(localFixture.Repository.Network.Remotes["origin"]);
                    localFixture.Repository.Checkout(advancedCommit.Sha);
                    localFixture.Repository.DumpGraph();
                    GitHelper.NormalizeGitDirectory(localFixture.RepositoryPath, new Authentication(), noFetch: false, currentBranch: "ref/heads/develop");

                    var normalisedBranch = localFixture.Repository.Branches["develop"];
                    normalisedBranch.ShouldNotBe(null);
                    fixture.Repository.DumpGraph();
                    localFixture.Repository.DumpGraph();
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

                fixture.Repository.Checkout(fixture.Repository.CreateBranch("develop"));
                var lastCommitOnDevelop = fixture.Repository.MakeACommit();

                fixture.Repository.Checkout(fixture.Repository.CreateBranch("feature/foo"));
                fixture.Repository.MakeACommit();

                using (var localFixture = fixture.CloneRepository())
                {
                    localFixture.Repository.Checkout("origin/develop");

                    // Another commit on feature/foo will force an update
                    fixture.Checkout("feature/foo");
                    fixture.Repository.MakeACommit();

                    GitHelper.NormalizeGitDirectory(localFixture.RepositoryPath, new Authentication(), noFetch: false, currentBranch: null);

                    localFixture.Repository.DumpGraph();
                    localFixture.Repository.Head.Tip.Sha.ShouldBe(lastCommitOnDevelop.Sha);
                }
            }
        }
    }
}