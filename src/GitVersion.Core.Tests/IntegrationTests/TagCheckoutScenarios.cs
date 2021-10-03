using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class TagCheckoutScenarios
{
    [Test]
    public void GivenARepositoryWithSingleCommit()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.Checkout(taggedVersion);

        fixture.AssertFullSemver(taggedVersion);
    }

    [Test]
    public void GivenARepositoryWithSingleCommitAndSingleBranch()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string taggedVersion = "1.0.3";
        fixture.Repository.MakeATaggedCommit(taggedVersion);
        fixture.BranchTo("task1");
        fixture.Checkout(taggedVersion);

        fixture.AssertFullSemver(taggedVersion);
    }

    [Test]
    public void GivenARepositoryWithTwoTagsAndADevelopBranch()
    {
        using var fixture = new EmptyRepositoryFixture();
        const string firstVersion = "1.0";
        const string hotfixVersion = "1.0.1";

        fixture.MakeACommit($"init {TestBase.MainBranch}");
        fixture.ApplyTag(firstVersion);
        fixture.MakeACommit("hotfix");
        fixture.ApplyTag(hotfixVersion);
        fixture.BranchTo("develop");
        fixture.MakeACommit("new feature");
        fixture.Checkout(hotfixVersion);
        fixture.BranchTo("tags/1.0.1");

        fixture.AssertFullSemver(hotfixVersion);
    }
}
