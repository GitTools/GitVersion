using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class OtherBranchTests
{
    [Test]
    public void CanTakeVersionFromReleaseBranch()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            const string TaggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("alpha-2.0.0");
            fixture.Repository.Checkout("alpha-2.0.0");

            fixture.AssertFullSemver("2.0.0-alpha.1+0");
        }
    }
}