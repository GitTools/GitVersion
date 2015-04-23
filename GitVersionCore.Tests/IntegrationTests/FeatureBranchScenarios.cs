using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class FeatureBranchScenarios
{
    [Test]
    public void ShouldNotUseNumberInFeatureBranchAsPreReleaseNumberOffDevelop()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.Checkout("develop");
            fixture.Repository.CreateBranch("feature/JIRA-123");
            fixture.Repository.Checkout("feature/JIRA-123");
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver("1.1.0-JIRA-123.1+5");
        }
    }

    [Test]
    public void ShouldNotUseNumberInFeatureBranchAsPreReleaseNumberOffMaster()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("feature/JIRA-123");
            fixture.Repository.Checkout("feature/JIRA-123");
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver("1.0.1-JIRA-123.1+5");
        }
    }

    [Test]
    public void TestFeatureBranch()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("feature-test");
            fixture.Repository.Checkout("feature-test");
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver("1.0.1-test.1+5");
        }
    }

    [Test]
    public void WhenTwoFeatureBranchPointToTheSameCommit()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.Checkout("develop");
            fixture.Repository.CreateBranch("feature/feature1");
            fixture.Repository.Checkout("feature/feature1");
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("feature/feature2");
            fixture.Repository.Checkout("feature/feature2");

            fixture.AssertFullSemver("0.1.0-feature2.1+1");
        }
    }
}