using GitTools.Testing;
using GitVersion.Model.Configuration;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.IntegrationTests;

/// <summary>
///     This demonstrates a full cycle to the release of 1.0.0 with the git flow workflow.
///     Since merging stuff to release/* from develop has a bug then decrements versions on develop
///     (as shown in the failing test <see cref="ReducedReleaseWorkflowDemo"/>),
///     we use cherry picking instead of merging to get stuff from develop to release/*.
///     For simplicity, we ignore the fact that the develop branch is usually updated via feature/* branches.
/// </summary>
public class FullReleaseGitFlowWithCherryPickWorkaround : IDisposable
{
    private readonly EmptyRepositoryFixture _fixture = new();

    private readonly Config _config = new()
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
        MakeACommit("Commit 1 (made on main)");
        CreateAndCheckoutBranch("develop");
        MakeACommit("Commit 2 (made on develop)");

        // ✅ succeeds as expected
        GetCurrentSemVer().ShouldBe("1.0.0-alpha.1");

        // now we are ready to start with the preparation of the 1.0.0 release
        CreateAndCheckoutBranch("release/1.0.0");

        // ✅ succeeds as expected
        GetCurrentSemVer().ShouldBe("1.0.0-beta.1");
        GetCurrentSemVer("develop").ShouldBe("1.0.0-alpha.1");

        // make another commit on release/1.0.0 to prepare the actual beta1 release
        MakeACommit("Commit 3 (made on release/1.0.0)");

        // ✅ succeeds as expected
        GetCurrentSemVer().ShouldBe("1.0.0-beta.1");
        GetCurrentSemVer("develop").ShouldBe("1.0.0-alpha.1");

        // now we makes changes on develop that may or may not end up in the 1.0.0 release
        CheckoutBranch("develop");
        MakeACommit("Commit 4 (made on develop)");

        // ✅ succeeds as expected
        GetCurrentSemVer().ShouldBe("1.1.0-alpha.1");
        GetCurrentSemVer("release/1.0.0").ShouldBe("1.0.0-beta.1");

        // now we do the actual release of beta 1
        CheckoutBranch("release/1.0.0");
        ApplyTag("1.0.0-beta1");

        // ✅ succeeds as expected
        GetCurrentSemVer().ShouldBe("1.0.0-beta.1");
        GetCurrentSemVer("develop").ShouldBe("1.1.0-alpha.1");

        // continue with more work on develop that may or may not end up in the 1.0.0 release
        CheckoutBranch("develop");
        MakeACommit("Commit 5 (made on develop)");

        // ✅ succeeds as expected
        GetCurrentSemVer().ShouldBe("1.1.0-alpha.2");
        GetCurrentSemVer("release/1.0.0").ShouldBe("1.0.0-beta.1");

        // now we decide that Commit 5 made on develop should be part of the beta 2 release
        // se we cherry pick it
        CheckoutBranch("release/1.0.0");
        CherryPickLatestCommitFromBranch("develop");

        // ✅ succeeds as expected
        GetCurrentSemVer().ShouldBe("1.0.0-beta.2");
        GetCurrentSemVer("develop").ShouldBe("1.1.0-alpha.2");

        // now we do an important bugfix that we found while preparing beta 2
        MakeACommit("Commit 6 (made on release/1.0.0)");

        // ✅ succeeds as expected
        GetCurrentSemVer().ShouldBe("1.0.0-beta.2");
        GetCurrentSemVer("develop").ShouldBe("1.1.0-alpha.2");

        // we want everything (Commit 3 and 6) that we made only on release/1.0.0 be in develop
        CheckoutBranch("develop");
        MergeWithNoFF("release/1.0.0");

        // ✅ succeeds as expected
        GetCurrentSemVer().ShouldBe("1.1.0-alpha.6");
        GetCurrentSemVer("release/1.0.0").ShouldBe("1.0.0-beta.2");

        // now we do the actual 1.0.0 release
        CheckoutBranch("release/1.0.0");
        MakeACommit("Commit 7 (made on release/1.0.0)");
        CheckoutBranch("main");
        MergeWithNoFF("release/1.0.0");
        ApplyTag("1.0.0");
        CheckoutBranch("develop");
        MergeWithNoFF("release/1.0.0");
        DeleteBranch("relesase/1.0.0");

        // ✅ succeeds as expected
        GetCurrentSemVer().ShouldBe("1.1.0-alpha.8");
        GetCurrentSemVer("main").ShouldBe("1.0.0");


    }

    private void DeleteBranch(string branch) => _fixture.Repository.Branches.Remove(branch);

    private void MakeACommit(string? message = null) => _fixture.Repository.MakeACommit(message);

    private void CheckoutBranch(string branchName) => Commands.Checkout(_fixture.Repository, _fixture.Repository.Branches[branchName]);

    private void CreateAndCheckoutBranch(string branchName) => Commands.Checkout(_fixture.Repository, _fixture.Repository.CreateBranch(branchName));

    private string GetCurrentSemVer(string? branch = null)
    {
        if (branch == null)
        {
            return _fixture.GetVersion(_config).SemVer;
        }

        if (_fixture.Repository.Branches.All(b => b.FriendlyName != branch))
        {
            throw new InvalidOperationException($"Branch {branch} does not exist");
        }

        return _fixture.GetVersion(_config, branch: branch).SemVer;
    }

    private void ApplyTag(string tag) => _fixture.Repository.ApplyTag(tag);

    private void MergeWithNoFF(string sourceBranch) => _fixture.Repository.MergeNoFF(sourceBranch);

    private void CherryPickLatestCommitFromBranch(string sourceBranch) => this._fixture.Repository.CherryPick(this._fixture.Repository.Branches[sourceBranch].Commits.First(), Generate.SignatureNow());
}
