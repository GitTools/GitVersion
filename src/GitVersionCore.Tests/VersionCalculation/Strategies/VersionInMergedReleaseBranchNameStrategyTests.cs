using System.Linq;

using GitTools.Testing;

using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;

using GitVersionCore.Tests.Helpers;

using LibGit2Sharp;

using NUnit.Framework;

using Shouldly;

namespace GitVersionCore.Tests.VersionCalculation.Strategies
{
    [TestFixture]
    public class VersionInMergedReleaseBranchNameStrategyTests : TestBase
    {
        [TestCase("release-2.0.0", "2.0.0")]
        [TestCase("release/3.0.0", "3.0.0")]
        public void CanTakeVersionFromNameOfReleaseBranch(string branchName, string expectedBaseVersion)
        {
            string mergedToBranchName = "master";
            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeACommit();
            fixture.BranchTo(branchName);
            fixture.MakeACommit();
            fixture.Checkout(mergedToBranchName);
            fixture.Repository.MergeNoFF(branchName, "a merge message");

            var strategy = GetVersionStrategy(fixture.RepositoryPath, fixture.Repository, mergedToBranchName);
            var baseVersion = strategy.GetVersions().Single();
            baseVersion.ShouldNotBeNull();
            baseVersion.SemanticVersion.ToString().ShouldBe(expectedBaseVersion);
        }

        [TestCase("hotfix/2.0.1", "2.0.1")]
        [TestCase("hotfix-3.0.1", "3.0.1")]
        public void CanTakeVersionFromNameOfConfiguredReleaseBranch(string branchName, string expectedBaseVersion)
        {
            string mergedToBranchName = "master";
            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeACommit();
            fixture.BranchTo(branchName);
            fixture.MakeACommit();
            fixture.Checkout(mergedToBranchName);
            fixture.Repository.MergeNoFF(branchName, "a merge message");

            var config = new ConfigurationBuilder()
                         .Add(new Config { Branches = { { "hotfix", new BranchConfig { IsReleaseBranch = true } } } })
                         .Build();

            var strategy = GetVersionStrategy(fixture.RepositoryPath, fixture.Repository, mergedToBranchName, config);

            var baseVersion = strategy.GetVersions().Single();

            baseVersion.SemanticVersion.ToString().ShouldBe(expectedBaseVersion);
        }

        private static IVersionStrategy GetVersionStrategy(string workingDirectory, IRepository repository, string branch, Config config = null)
        {
            var sp = BuildServiceProvider(workingDirectory, repository, branch, config);
            return sp.GetServiceForType<IVersionStrategy, VersionInMergedReleaseBranchNameStrategy>();
        }
    }

    internal static class IRepositoryExtensions
    {
        public static void MergeNoFF(this IRepository repository, string branchName, string message)
        {
            repository.Merge(branchName, Generate.SignatureNow(), new MergeOptions { CommitOnSuccess = false, FastForwardStrategy = FastForwardStrategy.NoFastForward });
            repository.Commit(message, Generate.SignatureNow(), Generate.SignatureNow());
        }
    }
}
