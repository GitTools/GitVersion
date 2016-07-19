using GitTools.Testing;
using GitVersion;
using GitVersionCore.Tests;
using LibGit2Sharp;
using NUnit.Framework;

public class MainlineDevelopmentMode
{
    private Config config = new Config
    {
        VersioningMode = VersioningMode.Mainline
    };

    [Test]
    public void MergedFeatureBranchesToMasterImpliesRelease()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeACommit("1");
            fixture.MakeACommit();

            fixture.BranchTo("feature/foo");
            fixture.Repository.MakeACommit("2");
            fixture.AssertFullSemver(config, "0.1.1-foo.1+2");
            fixture.Checkout("master");
            fixture.MergeNoFF("feature/foo");

            fixture.AssertFullSemver(config, "0.1.1+3");

            fixture.BranchTo("feature/foo2");
            fixture.Repository.MakeACommit("3 +semver: minor");
            fixture.AssertFullSemver(config, "0.2.0-foo2.1+4");
            fixture.Checkout("master");
            fixture.MergeNoFF("feature/foo2");
            fixture.AssertFullSemver(config, "0.2.0+5");

            fixture.BranchTo("feature/foo3");
            fixture.Repository.MakeACommit("4");
            fixture.Checkout("master");
            fixture.MergeNoFF("feature/foo3");
            var commit = fixture.Repository.Head.Tip;
            // Put semver increment in merge message
            fixture.Repository.Commit(commit.Message + " +semver: minor", commit.Author, commit.Committer, new CommitOptions
            {
                AmendPreviousCommit = true
            });
            commit = fixture.Repository.Head.Tip;
            fixture.AssertFullSemver(config, "0.3.0+7");

            fixture.BranchTo("feature/foo4");
            fixture.Repository.MakeACommit("5 +semver: major");
            fixture.AssertFullSemver(config, "1.0.0-foo4.1+8");
            fixture.Checkout("master");
            fixture.MergeNoFF("feature/foo4");
            fixture.AssertFullSemver(config, "1.0.0+9");
        }
    }

    // Write test which has a forward merge into a feature branch
}
