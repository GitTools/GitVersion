using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class FeatureBranchTests
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

            // Current output 1.1.0-JIRA-.123+5

            // A valid assert from my point of view
            fixture.AssertFullSemver("1.1.0-JIRA.123+5");

            // Or possible, but I'll guess it depends on wheter we're using a CD och CI flow?
            // fixture.AssertFullSemver("1.1.0-JIRA-123+5");

            // Or depending on how we treat the build number meta data
            // fixture.AssertFullSemver("1.1.0-JIRA-123.5+5");
        }
    }    
}