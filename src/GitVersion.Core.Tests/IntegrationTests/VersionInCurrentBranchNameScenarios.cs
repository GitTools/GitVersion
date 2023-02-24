using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using LibGit2Sharp;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class VersionInCurrentBranchNameScenarios : TestBase
{
    [Test]
    public void TakesVersionFromNameOfReleaseBranch()
    {
        using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
        fixture.BranchTo("release/2.0.0");

        fixture.AssertFullSemver("2.0.0-beta.1+0");
    }

    [Test]
    public void DoesNotTakeVersionFromNameOfNonReleaseBranch()
    {
        using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
        fixture.BranchTo("feature/upgrade-power-level-to-9000.0.1");

        fixture.AssertFullSemver("1.1.0-upgrade-power-level-to-9000-0-1.1+1");
    }

    [Test]
    public void TakesVersionFromNameOfBranchThatIsReleaseByConfig()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("support", builder => builder.WithIsReleaseBranch(true))
            .Build();

        using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
        fixture.BranchTo("support/2.0.0");

        fixture.AssertFullSemver("2.0.0+1", configuration);
    }

    [TestCase("origin")]
    [TestCase("upstream")]
    public void TakesVersionFromNameOfRemoteReleaseBranchInOrigin(string remoteNameInGit)
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithRemoteNameInGit(remoteNameInGit)
            .Build();

        using var fixture = new RemoteRepositoryFixture();
        if (remoteNameInGit != "origin")
        {
            fixture.LocalRepositoryFixture.Repository.Network.Remotes.Rename("origin", remoteNameInGit);
        }
        fixture.BranchTo("release/2.0.0");
        fixture.MakeACommit();
        Commands.Fetch((Repository)fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, Array.Empty<string>(), new FetchOptions(), null);

        fixture.LocalRepositoryFixture.Checkout($"{remoteNameInGit}/release/2.0.0");

        fixture.LocalRepositoryFixture.AssertFullSemver("2.0.0-beta.1+1", configuration);
    }

    [Test]
    public void DoesNotTakeVersionFromNameOfRemoteReleaseBranchInCustomRemote()
    {
        using var fixture = new RemoteRepositoryFixture();
        fixture.LocalRepositoryFixture.Repository.Network.Remotes.Rename("origin", "upstream");
        fixture.BranchTo("release/2.0.0");
        fixture.MakeACommit();
        Commands.Fetch((Repository)fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Network.Remotes.First().Name, Array.Empty<string>(), new FetchOptions(), null);

        fixture.LocalRepositoryFixture.Checkout("upstream/release/2.0.0");
        Action action = () => fixture.LocalRepositoryFixture.GetVersion();

        action.ShouldThrow<InvalidOperationException>();
    }
}
