namespace GitVersionCore.Tests.VersionCalculation.Strategies
{
    using System.Linq;
    using GitVersion;
    using GitVersion.VersionCalculation.BaseVersionCalculators;
    using LibGit2Sharp;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class VersionInBranchBaseVersionStrategyTests
    {
        [Test]
        [TestCase("release-2.0.0", "2.0.0")]
        [TestCase("release/2.0.0", "2.0.0")]
        [TestCase("hotfix-2.0.0", "2.0.0")]
        [TestCase("hotfix/2.0.0", "2.0.0")]
        [TestCase("hotfix/2.0.0", "2.0.0")]
        [TestCase("custom/JIRA-123", null)]
        public void CanTakeVersionFromBranchName(string branchName, string expectedBaseVersion)
        {
            var configuration = new Config();
            using (var fixture = new EmptyRepositoryFixture(configuration))
            {
                fixture.Repository.MakeACommit();
                var branch = fixture.Repository.CreateBranch(branchName);
                var sut = new VersionInBranchBaseVersionStrategy();

                var baseVersion = sut.GetVersions(new GitVersionContext(fixture.Repository, branch, configuration)).SingleOrDefault();

                if (expectedBaseVersion == null)
                    baseVersion.ShouldBe(null);
                else
                    baseVersion.SemanticVersion.ToString().ShouldBe(expectedBaseVersion);
            }
        }
    }
}
