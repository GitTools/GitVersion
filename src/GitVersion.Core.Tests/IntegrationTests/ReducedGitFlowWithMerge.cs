using GitTools.Testing;
using GitVersion.Model.Configuration;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.IntegrationTests;

/// <summary>
///     This demonstrates a bug that decrements the version on develop when we merge stuff from develop to release/*
///     in the default git flow workflow.
///     For simplicity, we ignore the fact that the develop branch is usually updated via feature/* branches.
/// </summary>
public class ReducedGitFlowWithMerge : IDisposable
{
    private static readonly EmptyRepositoryFixture _fixture = new();

    private static readonly Config _config = new()
    {
        // ❓ In my GitVersion.yml I actually have set the version to "1.0"
        // but that will cause an exception when I do it here in the tests
        NextVersion = "1.0.0"
    };

    public void Dispose() => _fixture.Dispose();

    [Test]
    public void Demonstrate()
    {
        // create main and develop branches
        // develop is one commits ahead of main
        MakeACommit();
        CreateAndCheckoutBranch("develop");
        MakeACommit();

        // ✅ succeeds as expected
        GetCurrentSemVer().ShouldBe("1.0.0-alpha.1");

        // now we are ready to start with the preparation of the 1.0.0 release
        CreateAndCheckoutBranch("release/1.0.0");

        // ✅ succeeds as expected
        GetCurrentSemVer().ShouldBe("1.0.0-beta.1");

        // make another commit on release/1.0.0 to prepare the actual beta1 release
        MakeACommit();

        // ✅ succeeds as expected
        GetCurrentSemVer().ShouldBe("1.0.0-beta.1");

        // now we makes changes on develop that may or may not end up in the 1.0.0 release
        CheckoutBranch("develop");
        MakeACommit();

        // ❌ fails! actual: "1.1.0-alpha.1"
        // We have not released 1.0.0, not even a beta, so why increment to 1.1.0?
        // Even though this surprising, it might actually be OK,
        // at least the version is incremented, which is needed for the CI nuget feed
        GetCurrentSemVer().ShouldBe("1.0.0-alpha.2");

        // now we do the actual release of beta 1
        CheckoutBranch("release/1.0.0");
        ApplyTag("1.0.0-beta1");

        // ✅ succeeds as expected
        GetCurrentSemVer().ShouldBe("1.0.0-beta.1");

        // continue with more work on develop that may or may not end up in the 1.0.0 release
        CheckoutBranch("develop");
        MakeACommit();

        // ❌ fails! actual: "1.1.0-alpha.2"
        // We still have not finally released 1.0.0 yet, only a beta, so why increment to 1.1.0?
        // Even though this surprising, it might actually be OK,
        // at least the version is incremented, which is needed for the CI nuget feed
        GetCurrentSemVer().ShouldBe("1.0.0-alpha.3");

        // now we decide that the new changes on develop should be part of the beta 2 release
        // se we merge it into release/1.0.0 with --no-ff because it is a protected branch
        // but we don't do the release of beta 2 jus yet
        CheckoutBranch("release/1.0.0");
        MergeWithNoFF("develop");

        // ✅ succeeds as expected
        GetCurrentSemVer().ShouldBe("1.0.0-beta.2");

        CheckoutBranch("develop");

        // ❌ fails! actual "1.0.0-alpha.3"
        // This is now really a problem. Why did it decrement the minor version?
        // All subsequent changes on develop will now a lower version that previously
        // and the nuget packages end up on the CI feed with a lower version
        // so users of that feed would need to _downgrade_ to get a _newer_ version.
        GetCurrentSemVer().ShouldBe("1.1.0-alpha.2");
    }

    private static void MakeACommit() => _fixture.Repository.MakeACommit();

    private void CheckoutBranch(string branchName) => Commands.Checkout(_fixture.Repository, _fixture.Repository.Branches[branchName]);

    private void CreateAndCheckoutBranch(string branchName) => Commands.Checkout(_fixture.Repository, _fixture.Repository.CreateBranch(branchName));

    private string GetCurrentSemVer() => _fixture.GetVersion(_config).SemVer;

    private void ApplyTag(string tag) => _fixture.Repository.ApplyTag(tag);

    private void MergeWithNoFF(string sourceBranch) => _fixture.Repository.MergeNoFF(sourceBranch);
}
