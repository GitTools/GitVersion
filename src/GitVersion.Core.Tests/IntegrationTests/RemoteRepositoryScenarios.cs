using GitVersion.Agents;
using GitVersion.Configuration;
using GitVersion.Testing.Extensions;
using LibGit2Sharp;

namespace GitVersion.Tests.IntegrationTests;

[TestFixture]
public class RemoteRepositoryScenarios : TestBase
{
    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    [TestCase(false, false, "tree", false)]
    [TestCase(true, false, "tree", false)]
    [TestCase(false, false, "blob", false)]
    [TestCase(true, false, "blob", false)]
    [TestCase(false, false, "tree", true)]
    [TestCase(true, false, "tree", true)]
    [TestCase(false, false, "blob", true)]
    [TestCase(true, false, "blob", true)]
    public void TaggedHistoricalCommitNormalizesWithoutAccessingRemote(bool annotated, bool multipleTags, string? metadataTarget = null, bool packed = false)
    {
        using var fixture = new RemoteRepositoryFixture(path =>
        {
            Repository.Init(path);
            var repository = new Repository(path);
            repository.MakeACommit();
            if (annotated)
            {
                repository.ApplyTag("1.2.3", repository.Head.Tip.Author, "Release");
            }
            else
            {
                repository.ApplyTag("1.2.3");
            }
            if (multipleTags)
            {
                repository.ApplyTag("release-alias");
            }
            repository.MakeACommit();
            repository.CreateBranch("feature/normalized");
            return repository;
        });
        var localRepository = fixture.LocalRepositoryFixture.Repository;
        if (metadataTarget != null)
        {
            using var content = new MemoryStream([1, 2, 3]);
            GitObject target = metadataTarget == "tree"
                ? localRepository.Head.Tip.Tree
                : localRepository.ObjectDatabase.CreateBlob(content);
            // Sort before the version tag so that the lookup must inspect this tag first.
            if (annotated)
            {
                localRepository.ApplyTag("!metadata", target.Sha, localRepository.Head.Tip.Author, "Metadata");
            }
            else
            {
                localRepository.ApplyTag("!metadata", target.Sha);
            }
        }
        if (packed)
        {
            GitTestExtensions.ExecuteGitCmd($"-C \"{fixture.LocalRepositoryFixture.RepositoryPath}\" pack-refs --all", ".");
        }
        var taggedCommit = (Commit)localRepository.Tags["1.2.3"].PeeledTarget;
        localRepository.Branches["feature/normalized"].ShouldBeNull();
        Commands.Checkout(localRepository, taggedCommit);
        localRepository.Network.Remotes.Update("origin", remote =>
            remote.Url = Path.Combine(fixture.LocalRepositoryFixture.RepositoryPath, "missing-remote"));

        var options = Options.Create(new GitVersionOptions
        {
            WorkingDirectory = fixture.LocalRepositoryFixture.RepositoryPath,
            Settings = { NoNormalize = false, NoFetch = true }
        });
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(GitHubActions.EnvironmentVariableName, "true");
        environment.SetEnvironmentVariable("GITHUB_REF_TYPE", "tag");
        environment.SetEnvironmentVariable("GITHUB_REF", "refs/tags/1.2.3");
        var sp = ConfigureServices(services =>
        {
            services.AddSingleton(options);
            services.AddSingleton<IEnvironment>(environment);
        });
        sp.DiscoverRepository();

        sp.GetRequiredService<IGitPreparer>().Prepare();

        localRepository.Head.Tip.Sha.ShouldBe(taggedCommit.Sha);
        localRepository.Info.IsHeadDetached.ShouldBeTrue();
        localRepository.Branches["feature/normalized"].Tip.Sha.ShouldBe(localRepository.Branches["origin/feature/normalized"].Tip.Sha);
        fixture.AssertFullSemver("1.2.3", repository: localRepository);
    }

    [TestCase("refs/pull/42/merge", 0, "pull/42/merge", "1.2.4-PullRequest42.0")]
    [TestCase(null, 1, "release/1.2.3", "1.3.0-beta.1+0")]
    [TestCase(null, 2, "main", "1.2.3")]
    public void TaggedCommitNormalizationPreservesBranchSelection(string? currentBranch, int localBranchCount, string expectedBranch, string expectedVersion)
    {
        using var fixture = new RemoteRepositoryFixture();
        var repository = fixture.LocalRepositoryFixture.Repository;
        var commit = repository.Head.Tip;
        repository.ApplyTag("1.2.3");
        repository.MakeACommit();
        Commands.Checkout(repository, commit);
        // Avoid updating main back to the tagged commit from its remote tracking ref.
        repository.Refs.Remove("refs/remotes/origin/main");
        if (localBranchCount > 0)
        {
            repository.CreateBranch("release/1.2.3", commit);
        }
        if (localBranchCount > 1)
        {
            repository.Refs.UpdateTarget(repository.Refs["refs/heads/main"], commit.Id);
        }
        repository.Network.Remotes.Update("origin", remote =>
            remote.Url = Path.Combine(fixture.LocalRepositoryFixture.RepositoryPath, "missing-remote"));

        PrepareOnGitHubActions(fixture.LocalRepositoryFixture.RepositoryPath, currentBranch);

        repository.Head.Tip.Sha.ShouldBe(commit.Sha);
        repository.Info.IsHeadDetached.ShouldBeFalse();
        repository.Head.FriendlyName.ShouldBe(expectedBranch);
        fixture.AssertFullSemver(expectedVersion, repository: repository);
    }

    [TestCase(null)]
    [TestCase("refs/tags/missing")]
    [TestCase("refs/tags/1.2.3")]
    public void HistoricalCommitWithoutMatchingBuildTagStillDiscoversPullRequest(string? currentTag)
    {
        using var fixture = new RemoteRepositoryFixture(path =>
        {
            Repository.Init(path);
            var repository = new Repository(path);
            repository.MakeACommit();
            repository.Refs.Add("refs/pull/42/merge", repository.Head.Tip.Id);
            repository.MakeATaggedCommit("1.2.3");
            return repository;
        });
        var localRepository = fixture.LocalRepositoryFixture.Repository;
        var commit = localRepository.Head.Tip.Parents.Single();
        Commands.Checkout(localRepository, commit);
        localRepository.ApplyTag("local-only");

        PrepareOnGitHubActions(fixture.LocalRepositoryFixture.RepositoryPath, null, currentTag);

        localRepository.Head.Tip.Sha.ShouldBe(commit.Sha);
        localRepository.Info.IsHeadDetached.ShouldBeFalse();
        localRepository.Head.FriendlyName.ShouldBe("pull/42/merge");
    }

    private static void PrepareOnGitHubActions(string workingDirectory, string? currentBranch, string? currentTag = null)
    {
        var options = Options.Create(new GitVersionOptions
        {
            WorkingDirectory = workingDirectory,
            Settings = { NoNormalize = false, NoFetch = true }
        });
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(GitHubActions.EnvironmentVariableName, "true");
        environment.SetEnvironmentVariable("GITHUB_REF", currentTag ?? currentBranch);
        environment.SetEnvironmentVariable("GITHUB_REF_TYPE", currentTag == null ? "branch" : "tag");
        var sp = ConfigureServices(services =>
        {
            services.AddSingleton(options);
            services.AddSingleton<IEnvironment>(environment);
        });
        sp.DiscoverRepository();
        sp.GetRequiredService<IGitPreparer>().Prepare();
    }

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

        sp.DiscoverRepository();

        var gitPreparer = sp.GetRequiredService<IGitPreparer>();

        gitPreparer.Prepare();

        fixture.AssertFullSemver("1.0.0-beta.1+10");
        fixture.AssertFullSemver("1.0.0-beta.1+10", repository: fixture.LocalRepositoryFixture.Repository);
    }

    [Test]
    public void GivenIgnoredRemoteBranch_WhenNormalizing_DoesNotCreateOrUpdateItsLocalBranch()
    {
        using var fixture = new RemoteRepositoryFixture(
            path =>
            {
                Repository.Init(path);
                var repository = new Repository(path);
                repository.MakeCommits(2);
                repository.CreateBranch("legacy/old");
                repository.CreateBranch("feature/keep");
                return repository;
            });

        var localRepository = fixture.LocalRepositoryFixture.Repository;
        var ignoredRemoteBranch = localRepository.Branches["origin/legacy/old"];
        ignoredRemoteBranch.ShouldNotBeNull();
        var ignoredLocalBranch = localRepository.CreateBranch("legacy/old", ignoredRemoteBranch.Tip);
        var originalIgnoredTip = ignoredLocalBranch.Tip.Sha;

        Commands.Checkout(fixture.Repository, "legacy/old");
        fixture.Repository.MakeACommit();

        var gitVersionOptions = new GitVersionOptions
        {
            WorkingDirectory = fixture.LocalRepositoryFixture.RepositoryPath,
            Settings = { NoNormalize = false, NoFetch = false }
        };
        var options = Options.Create(gitVersionOptions);
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(AzurePipelines.EnvironmentVariableName, "true");
        var configuration = GitFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Branches = ["^legacy/"] })
            .Build();
        var configurationProvider = Substitute.For<IConfigurationProvider>();
        configurationProvider.Provide(Arg.Any<IReadOnlyDictionary<object, object?>?>()).Returns(configuration);

        var sp = ConfigureServices(services =>
        {
            services.AddSingleton(options);
            services.AddSingleton<IEnvironment>(environment);
            services.AddSingleton(configurationProvider);
        });
        sp.DiscoverRepository();

        sp.GetRequiredService<IGitPreparer>().Prepare();

        localRepository.Branches["legacy/old"].Tip.Sha.ShouldBe(originalIgnoredTip);
        localRepository.Branches["feature/keep"].ShouldNotBeNull();
    }

    [Test]
    public void GivenTargetBranchMatchesIgnorePattern_WhenNormalizing_CreatesTargetBranch()
    {
        const string targetBranch = "legacy/target";
        using var fixture = new RemoteRepositoryFixture(
            path =>
            {
                Repository.Init(path);
                var repository = new Repository(path);
                repository.MakeCommits(2);
                repository.CreateBranch(targetBranch);
                return repository;
            });

        var gitVersionOptions = new GitVersionOptions
        {
            WorkingDirectory = fixture.LocalRepositoryFixture.RepositoryPath,
            RepositoryInfo = { TargetBranch = targetBranch },
            Settings = { NoNormalize = false, NoFetch = false }
        };
        var options = Options.Create(gitVersionOptions);
        var environment = new TestEnvironment();
        environment.SetEnvironmentVariable(AzurePipelines.EnvironmentVariableName, "true");
        var configuration = GitFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Branches = ["^legacy/"] })
            .Build();
        var configurationProvider = Substitute.For<IConfigurationProvider>();
        configurationProvider.Provide(Arg.Any<IReadOnlyDictionary<object, object?>?>()).Returns(configuration);

        var sp = ConfigureServices(services =>
        {
            services.AddSingleton(options);
            services.AddSingleton<IEnvironment>(environment);
            services.AddSingleton(configurationProvider);
        });
        sp.DiscoverRepository();

        sp.GetRequiredService<IGitPreparer>().Prepare();

        fixture.LocalRepositoryFixture.Repository.Branches[targetBranch].ShouldNotBeNull();
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

    private static void CopyRemoteBranchesToHeads(Repository repository)
    {
        foreach (var branch in repository.Branches)
        {
            if (!branch.IsRemote)
            {
                continue;
            }

            var localName = branch.FriendlyName.Replace($"{branch.RemoteName}/", "");
            if (repository.Branches[localName] == null)
            {
                repository.CreateBranch(localName, branch.FriendlyName);
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

        if (origin != "origin")
        {
            fixture.LocalRepositoryFixture.Repository.Network.Remotes.RenameRemote("origin", origin);
        }

        fixture.LocalRepositoryFixture.Fetch(origin);
        fixture.LocalRepositoryFixture.Checkout("develop");

        fixture.LocalRepositoryFixture.AssertFullSemver(expectedVersion);
    }
}
