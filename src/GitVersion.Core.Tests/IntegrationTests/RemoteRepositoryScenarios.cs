using GitVersion.Agents;
using GitVersion.Core.Tests.Helpers;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class RemoteRepositoryScenarios : TestBase
{
    [Test]
    public void GivenARemoteGitRepositoryWithCommitsThenClonedLocalShouldMatchRemoteVersion()
    {
        using var fixture = new RemoteRepositoryFixture();
        fixture.AssertFullSemver("0.0.1-5");
        fixture.AssertFullSemver("0.0.1-5", repository: fixture.LocalRepositoryFixture.Repository);
    }

    [Test]
    public void GivenARemoteGitRepositoryWithCommitsAndBranchesThenClonedLocalShouldMatchRemoteVersion()
    {
        const string targetBranch = "release-1.0.0";
        using var fixture = new RemoteRepositoryFixture(
            path =>
            {
                Repository.Init(path);
                Console.WriteLine("Created git repository at '{0}'", path);

                var repo = new Repository(path);
                repo.MakeCommits(5);

                repo.CreateBranch("develop");
                repo.CreateBranch(targetBranch);

                Commands.Checkout(repo, targetBranch);
                repo.MakeCommits(5);

                return repo;
            });

        var gitVersionOptions = new GitVersionOptions
        {
            WorkingDirectory = fixture.LocalRepositoryFixture.RepositoryPath,
            RepositoryInfo =
            {
                TargetBranch = targetBranch
            },

            Settings =
            {
                NoNormalize = false,
                NoFetch = false
            }
        };
        var options = Options.Create(gitVersionOptions);
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(AzurePipelines.EnvironmentVariableName, "true");

        var sp = ConfigureServices(services =>
        {
            services.AddSingleton(options);
            services.AddSingleton<IEnvironment>(environment);
        });

        var gitPreparer = sp.GetRequiredService<IGitPreparer>();

        gitPreparer.Prepare();

        fixture.AssertFullSemver("1.0.0-beta.1+10");
        fixture.AssertFullSemver("1.0.0-beta.1+10", repository: fixture.LocalRepositoryFixture.Repository);
    }

    [Test]
    public void GivenARemoteGitRepositoryAheadOfLocalRepositoryThenChangesShouldPull()
    {
        using var fixture = new RemoteRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.AssertFullSemver("0.0.1-6");
        fixture.AssertFullSemver("0.0.1-5", repository: fixture.LocalRepositoryFixture.Repository);
        var buildSignature = fixture.LocalRepositoryFixture.Repository.Config.BuildSignature(new(DateTime.Now));
        Commands.Pull(fixture.LocalRepositoryFixture.Repository, buildSignature, new());
        fixture.AssertFullSemver("0.0.1-6", repository: fixture.LocalRepositoryFixture.Repository);
    }

    [Test]
    public void GivenARemoteGitRepositoryWhenCheckingOutDetachedHeadUsingExistingImplementationHandleDetachedBranch()
    {
        using var fixture = new RemoteRepositoryFixture();
        Commands.Checkout(
            fixture.LocalRepositoryFixture.Repository,
            fixture.LocalRepositoryFixture.Repository.Head.Tip);

        fixture.AssertFullSemver("0.0.1--no-branch-.1+5", repository: fixture.LocalRepositoryFixture.Repository, onlyTrackedBranches: false);
    }

    [Test]
    public void GivenARemoteGitRepositoryWhenCheckingOutDetachedHeadUsingTrackingBranchOnlyBehaviourShouldReturnVersion014Plus5()
    {
        using var fixture = new RemoteRepositoryFixture();
        Commands.Checkout(fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Head.Tip);

        fixture.AssertFullSemver("0.0.1--no-branch-.1+5", repository: fixture.LocalRepositoryFixture.Repository);
    }

    [Test]
    public void GivenARemoteGitRepositoryTheLocalAndRemoteBranchAreTreatedAsSameParentWhenInheritingConfiguration()
    {
        using var remote = new EmptyRepositoryFixture();
        remote.MakeATaggedCommit("1.0");
        remote.BranchTo("develop");
        remote.MakeACommit();
        remote.Checkout("main");
        remote.BranchTo("support/1.0.x");
        remote.MakeATaggedCommit("1.0.1");

        using var local = remote.CloneRepository();
        CopyRemoteBranchesToHeads(local.Repository);
        local.BranchTo("bug/hotfix");
        local.MakeACommit();
        local.AssertFullSemver("1.0.2-bug-hotfix.1+1");
    }

    private static void CopyRemoteBranchesToHeads(IRepository repository)
    {
        foreach (var branch in repository.Branches)
        {
            if (branch.IsRemote)
            {
                var localName = branch.FriendlyName.Replace($"{branch.RemoteName}/", "");
                if (repository.Branches[localName] == null)
                {
                    repository.CreateBranch(localName, branch.FriendlyName);
                }
            }
        }
    }

    [TestCase("origin", "release-2.0.0", "2.1.0-alpha.0")]
    [TestCase("custom", "release-2.0.0", "0.1.0-alpha.5")]
    [TestCase("origin", "release/3.0.0", "3.1.0-alpha.0")]
    [TestCase("custom", "release/3.0.0", "0.1.0-alpha.5")]
    public void EnsureRemoteReleaseBranchesAreTracked(string origin, string branchName, string expectedVersion)
    {
        using var fixture = new RemoteRepositoryFixture("develop");

        fixture.CreateBranch(branchName);
        fixture.MakeACommit();

        if (origin != "origin") fixture.LocalRepositoryFixture.Repository.Network.Remotes.Rename("origin", origin);
        fixture.LocalRepositoryFixture.Fetch(origin);
        fixture.LocalRepositoryFixture.Checkout("develop");

        fixture.LocalRepositoryFixture.AssertFullSemver(expectedVersion);
    }
}
