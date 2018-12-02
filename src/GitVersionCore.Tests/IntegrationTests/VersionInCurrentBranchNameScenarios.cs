namespace GitVersionCore.Tests.IntegrationTests
{
    using System.Collections.Generic;

    using GitTools.Testing;

    using GitVersion;

    using GitVersionCore.Tests;
    using NUnit.Framework;

    [TestFixture]
    public class VersionInCurrentBranchNameScenarios : TestBase
    {
        [Test]
        public void TakesVersionFromNameOfAnyBranchByDefault()
        {
            using (var fixture = new BaseGitFlowRepositoryFixture("1.0.0"))
            {
                fixture.BranchTo("feature/upgrade-power-level-to-9000.0.1");

                fixture.AssertFullSemver("9000.0.1-upgrade-power-level-to.1+0");
            }
        }

        [Test]
        public void TakesVersionOnlyFromNameOfReleaseBranchByConfig()
        {
            var config = new Config { Ignore = new IgnoreConfig { NonReleaseBranches = true } };

            using (var fixture = new BaseGitFlowRepositoryFixture("1.0.0"))
            {
                fixture.BranchTo("feature/upgrade-power-level-to-9000.0.1");

                fixture.AssertFullSemver(config, "1.1.0-upgrade-power-level-to-9000-0-1.1+1");
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
                fixture.BranchTo("support/2.0.0");

                fixture.AssertFullSemver(config, "2.0.0+1");
            }
        }
    }
}
