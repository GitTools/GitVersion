namespace GitVersionCore.Tests.VersionCalculation.Strategies
{
    using System.Linq;
    using GitTools.Testing;
    using GitVersion;
    using GitVersion.VersionCalculation.BaseVersionCalculators;
    using LibGit2Sharp;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class VersionInBranchNameBaseVersionStrategyTests : TestBase
    {
        [TestCase("release-2.0.0", "2.0.0")]
        [TestCase("release/2.0.0", "2.0.0")]
        [TestCase("hotfix-2.0.0", "2.0.0")]
        [TestCase("hotfix/2.0.0", "2.0.0")]
        [TestCase("custom/JIRA-123", null)]
        [TestCase("hotfix/downgrade-to-gitversion-3.6.5-to-fix-miscalculated-version", "3.6.5")]
        public void CanTakeVersionFromBranchName(string branchName, string expectedBaseVersion)
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeACommit();
                var branch = fixture.Repository.CreateBranch(branchName);
                var sut = new VersionInBranchNameBaseVersionStrategy();

                var gitVersionContext = new GitVersionContext(fixture.Repository, branch, new Config().ApplyDefaults());
                var baseVersion = sut.GetVersions(gitVersionContext).SingleOrDefault();

                if (expectedBaseVersion == null)
                    baseVersion.ShouldBe(null);
                else
                    baseVersion.SemanticVersion.ToString().ShouldBe(expectedBaseVersion);
            }
        }

        [TestCase("feature/upgrade-to-gitversion-4.0.0", null)]
        [TestCase("hotfix/downgrade-to-gitversion-3.6.5-to-fix-miscalculated-version", null)]
        [TestCase("hotfix/2.0.0", null)]
        [TestCase("custom/JIRA-123", null)]
        [TestCase("release/2.0.0", "2.0.0")]
        public void ShouldNotTakeVersionFromNameOfIgnoredBranches(string branchName, string expectedBaseVersion)
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeACommit();
                var branch = fixture.Repository.CreateBranch(branchName);
                var sut = new VersionInBranchNameBaseVersionStrategy();

                var config = new Config { Ignore = new IgnoreConfig { NonReleaseBranches = true } }.ApplyDefaults();
                var gitVersionContext = new GitVersionContext(fixture.Repository, branch, config);
                var baseVersion = sut.GetVersions(gitVersionContext).SingleOrDefault();

                if (expectedBaseVersion == null)
                    baseVersion.ShouldBe(null);
                else
                    baseVersion.SemanticVersion.ToString().ShouldBe(expectedBaseVersion);
            }
        }
    }
}
