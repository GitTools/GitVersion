using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class GitFlowFeatureBranchTests
{
    [Test]
    public void ShouldNotUseNumberInFeatureBranchAsPreReleaseNumber()
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
}