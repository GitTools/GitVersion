using GitVersion.Configuration;
using GitVersion.Core.Tests;
using GitVersion.Core.Tests.IntegrationTests;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.TrunkBased;

internal partial class TrunkBasedScenariosWithAGitHubFlow
{
    [Parallelizable(ParallelScope.All)]
    public class GivenAFeatureBranchWithTwoCommitsWhenFirstCommitTaggedAsPreRelease
    {
        private EmptyRepositoryFixture? fixture;

        private static GitHubFlowConfigurationBuilder TrunkBasedBuilder => GitHubFlowConfigurationBuilder.New
            .WithVersionStrategy(VersionStrategies.TrunkBased).WithLabel(null)
            .WithBranch("feature", _ => _
                .WithDeploymentMode(DeploymentMode.ManualDeployment).WithPreventIncrementWhenCurrentCommitTagged(true)
            );

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // B 58 minutes ago (HEAD -> feature/foo)
            // A 59 minutes ago (tag 0.0.3-4)

            fixture = new EmptyRepositoryFixture("feature/foo");

            fixture.MakeACommit("A");
            fixture.ApplyTag("0.0.3-4");
            fixture.MakeACommit("B");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => fixture?.Dispose();

        [TestCase(IncrementStrategy.None, null, ExpectedResult = "0.0.3-5+1")]
        [TestCase(IncrementStrategy.Patch, null, ExpectedResult = "0.0.3-5+1")]
        [TestCase(IncrementStrategy.Minor, null, ExpectedResult = "0.0.3-5+1")]
        [TestCase(IncrementStrategy.Major, null, ExpectedResult = "0.0.3-5+1")]

        [TestCase(IncrementStrategy.None, "", ExpectedResult = "0.0.3-5+1")]
        [TestCase(IncrementStrategy.Patch, "", ExpectedResult = "0.0.3-5+1")]
        [TestCase(IncrementStrategy.Minor, "", ExpectedResult = "0.0.3-5+1")]
        [TestCase(IncrementStrategy.Major, "", ExpectedResult = "0.0.3-5+1")]

        [TestCase(IncrementStrategy.None, "foo", ExpectedResult = "0.0.3-foo.1+2")]
        [TestCase(IncrementStrategy.Patch, "foo", ExpectedResult = "0.0.3-foo.1+2")]
        [TestCase(IncrementStrategy.Minor, "foo", ExpectedResult = "0.1.0-foo.1+2")]
        [TestCase(IncrementStrategy.Major, "foo", ExpectedResult = "1.0.0-foo.1+2")]

        [TestCase(IncrementStrategy.None, "bar", ExpectedResult = "0.0.3-bar.1+2")]
        [TestCase(IncrementStrategy.Patch, "bar", ExpectedResult = "0.0.3-bar.1+2")]
        [TestCase(IncrementStrategy.Minor, "bar", ExpectedResult = "0.1.0-bar.1+2")]
        [TestCase(IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+2")]
        public string GetVersion(IncrementStrategy increment, string? label)
        {
            IGitVersionConfiguration trunkBased = TrunkBasedBuilder
                .WithBranch("feature", _ => _.WithIncrement(increment).WithLabel(label))
                .Build();

            return fixture!.GetVersion(trunkBased).FullSemVer;
        }
    }
}
