using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class WorktreeScenarios : TestBase
{
    [Test]
    public void UseWorktreeRepositoryForVersion()
    {
        using var fixture = new EmptyRepositoryFixture();
        var repoDir = new DirectoryInfo(fixture.RepositoryPath);

        repoDir.Parent.ShouldNotBeNull();
        var worktreePath = PathHelper.Combine(repoDir.Parent.FullName, $"{repoDir.Name}-v1");

        fixture.Repository.MakeATaggedCommit("v1.0.0");
        var branchV1 = fixture.Repository.CreateBranch("support/1.0");

        fixture.Repository.MakeATaggedCommit("v2.0.0");
        fixture.AssertFullSemver("2.0.0");

        fixture.Repository.Worktrees.Add(branchV1.CanonicalName, "1.0", worktreePath, false);
        using var worktreeFixture = new LocalRepositoryFixture(new Repository(worktreePath));
        worktreeFixture.AssertFullSemver("1.0.0");
    }
}
