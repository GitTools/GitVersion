namespace GitVersionCore.Tests
{
    using GitVersion;
    using LibGit2Sharp;
    using NUnit.Framework;
    using Shouldly;

    public class GitHelperTests
    {
        [Test]
        public void NormalisationOfPullRequestsWithFetch()
        {
            using (var fixture = new EmptyRepositoryFixture(new Config()))
            {
                fixture.Repository.MakeACommit();

                fixture.Repository.CreateBranch("feature/foo").Checkout();
                fixture.Repository.MakeACommit();
                var commit = fixture.Repository.CreatePullRequestRef("feature/foo", "master", prNumber: 3);
                using (var localFixture = fixture.CloneRepository())
                {
                    localFixture.Checkout(commit.Sha);
                    GitHelper.NormalizeGitDirectory(localFixture.RepositoryPath, new Authentication(), noFetch: false, currentBranch: string.Empty);

                    var normalisedPullBranch = localFixture.Repository.FindBranch("pull/3/merge");
                    normalisedPullBranch.ShouldNotBe(null);
                }
            }
        }

        [Test]
        public void NormalisationOfPullRequestsWithoutFetch()
        {
            using (var fixture = new EmptyRepositoryFixture(new Config()))
            {
                fixture.Repository.MakeACommit();

                fixture.Repository.CreateBranch("feature/foo").Checkout();
                fixture.Repository.MakeACommit();
                var commit = fixture.Repository.CreatePullRequestRef("feature/foo", "master", prNumber: 3, allowFastFowardMerge: true);
                using (var localFixture = fixture.CloneRepository())
                {
                    localFixture.Checkout(commit.Sha);
                    GitHelper.NormalizeGitDirectory(localFixture.RepositoryPath, new Authentication(), noFetch: true, currentBranch: "refs/pull/3/merge");

                    var normalisedPullBranch = localFixture.Repository.FindBranch("pull/3/merge");
                    normalisedPullBranch.ShouldNotBe(null);
                }
            }
        }

        [Test]
        public void UpdatesLocalBranchesWhen()
        {
            using (var fixture = new EmptyRepositoryFixture(new Config()))
            {
                fixture.Repository.MakeACommit();

                fixture.Repository.CreateBranch("feature/foo").Checkout();
                fixture.Repository.MakeACommit();
                using (var localFixture = fixture.CloneRepository())
                {
                    localFixture.Checkout("feature/foo");
                    // Advance remote
                    var advancedCommit = fixture.Repository.MakeACommit();
                    GitHelper.NormalizeGitDirectory(localFixture.RepositoryPath, new Authentication(), noFetch: false, currentBranch: null);

                    var normalisedBranch = localFixture.Repository.FindBranch("feature/foo");
                    normalisedBranch.ShouldNotBe(null);
                    normalisedBranch.Tip.Sha.ShouldBe(advancedCommit.Sha);
                }
            }
        }
    }
}