using System;
using GitTools.Testing;
using GitVersion;
using GitVersion.BuildAgents;
using GitVersionCore.Tests.Helpers;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests.IntegrationTests
{
    [TestFixture]
    public class RemoteRepositoryScenarios : TestBase
    {
        [Test]
        public void GivenARemoteGitRepositoryWithCommitsThenClonedLocalShouldMatchRemoteVersion()
        {
            using var fixture = new RemoteRepositoryFixture();
            fixture.AssertFullSemver("0.1.0+4");
            fixture.AssertFullSemver("0.1.0+4", repository: fixture.LocalRepositoryFixture.Repository);
        }

        [Test]
        public void GivenARemoteGitRepositoryWithCommitsAndBranchesThenClonedLocalShouldMatchRemoteVersion()
        {
            var targetBranch = "release-1.0";
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

            var gitPreparer = sp.GetService<IGitPreparer>();

            gitPreparer.Prepare();

            fixture.AssertFullSemver("1.0.0-beta.1+5");
            fixture.AssertFullSemver("1.0.0-beta.1+5", repository: fixture.LocalRepositoryFixture.Repository);
        }

        [Test]
        public void GivenARemoteGitRepositoryAheadOfLocalRepositoryThenChangesShouldPull()
        {
            using var fixture = new RemoteRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("0.1.0+5");
            fixture.AssertFullSemver("0.1.0+4", repository: fixture.LocalRepositoryFixture.Repository);
            var buildSignature = fixture.LocalRepositoryFixture.Repository.Config.BuildSignature(new DateTimeOffset(DateTime.Now));
            Commands.Pull((Repository)fixture.LocalRepositoryFixture.Repository, buildSignature, new PullOptions());
            fixture.AssertFullSemver("0.1.0+5", repository: fixture.LocalRepositoryFixture.Repository);
        }

        [Test]
        public void GivenARemoteGitRepositoryWhenCheckingOutDetachedheadUsingExistingImplementationThrowsException()
        {
            using var fixture = new RemoteRepositoryFixture();
            Commands.Checkout(
                fixture.LocalRepositoryFixture.Repository,
                fixture.LocalRepositoryFixture.Repository.Head.Tip);

            Should.Throw<WarningException>(() => fixture.AssertFullSemver("0.1.0+4", repository: fixture.LocalRepositoryFixture.Repository, onlyTrackedBranches: false),
                $"It looks like the branch being examined is a detached Head pointing to commit '{fixture.LocalRepositoryFixture.Repository.Head.Tip.Id.ToString(7)}'. Without a proper branch name GitVersion cannot determine the build version.");
        }

        [Test]
        public void GivenARemoteGitRepositoryWhenCheckingOutDetachedheadUsingTrackingBranchOnlyBehaviourShouldReturnVersion014Plus5()
        {
            using var fixture = new RemoteRepositoryFixture();
            Commands.Checkout(fixture.LocalRepositoryFixture.Repository, fixture.LocalRepositoryFixture.Repository.Head.Tip);

            fixture.AssertFullSemver("0.1.0+4", repository: fixture.LocalRepositoryFixture.Repository);
        }
    }
}
