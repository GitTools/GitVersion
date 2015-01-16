namespace GitVersionCore.Tests.VersionCalculation.Strategies
{
    using GitVersion.VersionCalculation.BaseVersionCalculators;
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
            var context = new GitVersionContextBuilder()
                .WithBranch(branchName)
                .AddCommit()
                .Build();

            var sut = new VersionInBranchBaseVersionStrategy();

            var baseVersion = sut.GetVersion(context);

            if (expectedBaseVersion == null)
                baseVersion.ShouldBe(null);
            else
                baseVersion.SemanticVersion.ToString().ShouldBe(expectedBaseVersion);
        }
    }
}
