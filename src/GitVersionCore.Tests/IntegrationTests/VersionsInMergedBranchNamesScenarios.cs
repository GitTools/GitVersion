namespace GitVersionCore.Tests.IntegrationTests
{
    using System.Collections.Generic;

    using GitTools.Testing;

    using GitVersion;

    using GitVersionCore.Tests;

    using NUnit.Framework;

    [TestFixture]
    public class VersionsInMergedBranchNamesScenarios : TestBase
    {
        [Test]
        public void TakesVersionFromNameOfAnyBranchByDefault()
        {
            using (var fixture = new BaseGitFlowRepositoryFixture("1.0.0"))
            {
                fixture.CreateAndMergeBranchIntoDevelop("feature/upgrade-power-level-to-9000.0.1");

                fixture.AssertFullSemver("9000.1.0-alpha.0");
            }
        }

        [Test]
        public void TakesVersionOnlyFromNameOfReleaseBranchByConfig()
        {
            var config = new Config { Ignore = new IgnoreConfig { NonReleaseBranches = true } };

            using (var fixture = new BaseGitFlowRepositoryFixture("1.0.0"))
            {
                fixture.CreateAndMergeBranchIntoDevelop("pull-request/improved-by-upgrading-some-lib-to-4.5.6");
                fixture.CreateAndMergeBranchIntoDevelop("release/2.0.0");
                fixture.CreateAndMergeBranchIntoDevelop("hotfix/downgrade-some-lib-to-3.2.1-to-avoid-breaking-changes");

                fixture.AssertFullSemver(config, "2.1.0-alpha.4");
            }
        }

        [Test]
        public void TakesVersionFromNameOfBranchThatIsReleaseByConfig()
        {
            var config = new Config
            {
                Ignore = new IgnoreConfig { NonReleaseBranches = true },
                Branches = new Dictionary<string, BranchConfig> { { "support", new BranchConfig { IsReleaseBranch = true } } }
            };

            using (var fixture = new BaseGitFlowRepositoryFixture("1.0.0"))
            {
                fixture.CreateAndMergeBranchIntoDevelop("support/2.0.0");

                fixture.AssertFullSemver(config, "2.1.0-alpha.2");
            }
        }
    }

    internal static class BaseGitFlowRepositoryFixtureExtensions
    {
        public static void CreateAndMergeBranchIntoDevelop(this BaseGitFlowRepositoryFixture fixture, string branchName)
        {
            fixture.BranchTo(branchName);
            fixture.MakeACommit();
            fixture.Checkout("develop");
            fixture.MergeNoFF(branchName);
        }
    }
}
