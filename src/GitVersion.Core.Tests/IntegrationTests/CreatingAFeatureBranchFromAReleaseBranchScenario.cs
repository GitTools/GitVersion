using GitTools.Testing;
using GitVersion.Core.Tests.Helpers;
using NUnit.Framework;

namespace GitVersion.Core.Tests.IntegrationTests;

/// <summary>
/// Version not generated correct when creating a feature branch from a release branch #3101
/// </summary>
[TestFixture]
public class CreatingAFeatureBranchFromAReleaseBranchScenario
{
    [TestCase("main")]
    [TestCase("develop")]
    public void __Just_A_Test_1__(string branchName)
    {
        var configuration = ConfigBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture(branchName);
        fixture.Repository.MakeACommit();
        fixture.BranchTo("release/1.0.0");

        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);
        fixture.BranchTo("hotfix/beta");
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);

        fixture.Checkout(branchName);
        fixture.MakeACommit();
        fixture.BranchTo("release/1.1.0");
        fixture.Checkout("release/1.0.0");

        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);
        fixture.Checkout("hotfix/beta");
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);

        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);
        fixture.Checkout("release/1.0.0");
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);
        fixture.MergeNoFF("hotfix/beta");
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);
        fixture.Repository.Branches.Remove("hotfix/beta");
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);
    }

    [TestCase("main")]
    [TestCase("develop")]
    public void __Just_A_Test_2__(string branchName)
    {
        var configuration = ConfigBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture(branchName);
        fixture.Repository.MakeACommit();

        fixture.BranchTo("release/1.0.0");
        fixture.Repository.MakeACommit();

        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

        fixture.BranchTo("feature/just-a-test");
        fixture.AssertFullSemver("1.0.0-just-a-test.1+1", configuration);

        fixture.Checkout(branchName);
        fixture.MakeACommit();
        fixture.BranchTo("release/1.1.0");
        fixture.Checkout("release/1.0.0");

        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);
        fixture.Checkout("feature/just-a-test");
        fixture.AssertFullSemver("1.0.0-just-a-test.1+1", configuration);

        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-just-a-test.1+2", configuration);
        fixture.Checkout("release/1.0.0");
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);
        fixture.MergeNoFF("feature/just-a-test");
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);
        fixture.Repository.Branches.Remove("feature/just-a-test");
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);
    }

    [TestCase("main")]
    [TestCase("develop")]
    public void __Just_A_Test_3__(string branchName)
    {
        var configuration = ConfigBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture(branchName);
        fixture.Repository.MakeACommit();

        fixture.BranchTo("release/1.0.0");
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);

        fixture.BranchTo("hotfix/just-a-test");
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);

        fixture.Checkout(branchName);
        fixture.MakeACommit();
        fixture.BranchTo("release/1.1.0");
        fixture.Checkout("release/1.0.0");

        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);
        fixture.Checkout("hotfix/just-a-test");
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);

        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);
        fixture.Checkout("release/1.0.0");
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);
        fixture.MergeNoFF("hotfix/just-a-test");
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);
        fixture.Repository.Branches.Remove("hotfix/just-a-test");
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);
    }

    [Test]
    public void __Just_A_Test_41__()
    {
        var configuration = ConfigBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture("main");
        fixture.Repository.MakeACommit();

        fixture.BranchTo("release/1.0.0");
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);

        fixture.BranchTo("feature/just-a-test");
        fixture.AssertFullSemver("1.0.0-just-a-test.1+0", configuration);

        fixture.Checkout("main");
        fixture.MakeACommit();
        fixture.BranchTo("release/1.1.0");
        fixture.Checkout("release/1.0.0");

        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);
        fixture.Checkout("feature/just-a-test");
        fixture.AssertFullSemver("1.0.0-just-a-test.1+0", configuration);

        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-just-a-test.1+1", configuration);
        fixture.Checkout("release/1.0.0");
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);
        fixture.MergeNoFF("feature/just-a-test");
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);
        fixture.Repository.Branches.Remove("feature/just-a-test");
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);
    }

    [Test]
    public void __Just_A_Test_42__()
    {
        var configuration = ConfigBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture("develop");
        fixture.Repository.MakeACommit();

        fixture.BranchTo("release/1.0.0");

        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);

        // 1.1.0 is correct because the base branch points to develop and release
        // maybe we can fix it is configurable with PreReleaseWeight?
        fixture.BranchTo("feature/just-a-test");
        fixture.AssertFullSemver("1.1.0-just-a-test.1+0", configuration);

        fixture.Checkout("develop");
        fixture.MakeACommit();
        fixture.BranchTo("release/1.1.0");
        fixture.Checkout("release/1.0.0");

        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);
        fixture.Checkout("feature/just-a-test");
        fixture.AssertFullSemver("1.0.0-just-a-test.1+0", configuration); // 1.1.0-just-a-test.1+0

        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-just-a-test.1+1", configuration);
        fixture.Checkout("release/1.0.0");
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);
        fixture.MergeNoFF("feature/just-a-test");
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);
        fixture.Repository.Branches.Remove("feature/just-a-test");
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);
    }

    [Test]
    public void __Just_A_Test_5__()
    {
        var configuration = ConfigBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("0.0.1+1", configuration);
        fixture.BranchTo("release/1.0.0");
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);
        fixture.BranchTo("feature/just-a-test");
        fixture.AssertFullSemver("1.0.0-just-a-test.1+1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-just-a-test.1+2", configuration);
        fixture.Checkout("release/1.0.0");
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);
        fixture.MergeNoFF("feature/just-a-test");
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);
        fixture.Repository.Branches.Remove("feature/just-a-test");
        fixture.AssertFullSemver("1.0.0-beta.1+3", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1+4", configuration);
    }

    [Test]
    public void __Just_A_Test_6__()
    {
        var configuration = ConfigBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.0.1+1", configuration);
        fixture.BranchTo("develop");
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.1.0-alpha.2", configuration);
        fixture.BranchTo("release/1.0.0");
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);
        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.1.0-alpha.0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);
        fixture.MergeNoFF("release/1.0.0");
        fixture.AssertFullSemver("1.1.0-alpha.4", configuration);
        fixture.Repository.Branches.Remove("release/1.0.0");
        fixture.AssertFullSemver("1.1.0-alpha.4", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-alpha.5", configuration);
    }

    [Test]
    public void __Just_A_Test_7__()
    {
        var configuration = ConfigBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.0.1+1", configuration);
        fixture.BranchTo("develop");
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.1.0-alpha.2", configuration);
        fixture.BranchTo("release/1.0.0");
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);
        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.1.0-alpha.0", configuration);

        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        fixture.Checkout("main");
        fixture.MergeNoFF("release/1.0.0");
        fixture.AssertFullSemver("1.0.0+0", configuration);
        fixture.ApplyTag("1.0.0");
        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);
        fixture.MergeNoFF("main");
        fixture.AssertFullSemver("1.1.0-alpha.6", configuration);

        fixture.Repository.Branches.Remove("release/1.0.0");
        fixture.AssertFullSemver("1.1.0-alpha.6", configuration);
    }

    [Test]
    public void __Just_A_Test_8__()
    {
        var configuration = ConfigBuilder.New.Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.0.1+1", configuration);
        fixture.BranchTo("develop");
        fixture.AssertFullSemver("0.1.0-alpha.1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.1.0-alpha.2", configuration);
        fixture.BranchTo("release/1.0.0");
        fixture.AssertFullSemver("1.0.0-beta.1+0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.0-beta.1+2", configuration);
        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.1.0-alpha.0", configuration);

        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        fixture.Checkout("main");
        fixture.MergeNoFF("release/1.0.0");
        fixture.AssertFullSemver("1.0.0+0", configuration);
        fixture.ApplyTag("1.0.0");
        fixture.Repository.Branches.Remove("release/1.0.0");

        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.Checkout("develop");
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);
        fixture.MergeNoFF("main");
        fixture.AssertFullSemver("1.1.0-alpha.6", configuration);
    }
}
