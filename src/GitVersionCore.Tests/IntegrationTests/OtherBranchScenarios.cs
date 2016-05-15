using GitTools.Testing;
using GitVersionCore.Tests;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class OtherBranchScenarios
{
    [Test]
    public void CanTakeVersionFromReleaseBranch()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            const string TaggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("alpha-2.0.0");
            fixture.Repository.Checkout("alpha-2.0.0");

            fixture.AssertFullSemver("2.0.0-alpha.1+0");
        }
    }
    [Test]
    public void BranchesWithIllegalCharsShouldNotBeUsedInVersionNames()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            const string TaggedVersion = "1.0.3";
            fixture.Repository.MakeATaggedCommit(TaggedVersion);
            fixture.Repository.MakeCommits(5);
            fixture.Repository.CreateBranch("issue/m/github-569");
            fixture.Repository.Checkout("issue/m/github-569");

            fixture.AssertFullSemver("1.0.4-issue-m-github-569.1+5");
        }
    }
}