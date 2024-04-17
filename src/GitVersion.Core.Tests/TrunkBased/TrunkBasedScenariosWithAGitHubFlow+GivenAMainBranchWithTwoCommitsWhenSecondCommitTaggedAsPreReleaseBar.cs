using GitVersion.Configuration;
using GitVersion.Core.Tests;
using GitVersion.Core.Tests.IntegrationTests;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.TrunkBased;

internal partial class TrunkBasedScenariosWithAGitHubFlow
{
    [Parallelizable(ParallelScope.All)]
    public class GivenAMainBranchWithTwoCommitsWhenSecondCommitTaggedAsPreReleaseBar
    {
        private EmptyRepositoryFixture? fixture;

        private static GitHubFlowConfigurationBuilder TrunkBasedBuilder => GitHubFlowConfigurationBuilder.New
            .WithVersionStrategy(VersionStrategies.TrunkBased).WithLabel(null)
            .WithBranch("main", _ => _.WithDeploymentMode(DeploymentMode.ManualDeployment));

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // B 58 minutes ago  (HEAD -> main) (tag 0.2.0-bar)
            // A 59 minutes ago

            fixture = new EmptyRepositoryFixture();

            fixture.MakeACommit("A");
            fixture.MakeACommit("B");
            fixture.ApplyTag("0.2.0-bar");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => fixture?.Dispose();

        [TestCase(IncrementStrategy.None, null, ExpectedResult = "0.2.0-bar")]
        [TestCase(IncrementStrategy.Patch, null, ExpectedResult = "0.2.0-bar")]
        [TestCase(IncrementStrategy.Minor, null, ExpectedResult = "0.2.0-bar")]
        [TestCase(IncrementStrategy.Major, null, ExpectedResult = "0.2.0-bar")]

        [TestCase(IncrementStrategy.None, "", ExpectedResult = "0.2.0-1+1")]
        [TestCase(IncrementStrategy.Patch, "", ExpectedResult = "0.2.0-1+1")]
        [TestCase(IncrementStrategy.Minor, "", ExpectedResult = "0.2.0-1+1")]
        [TestCase(IncrementStrategy.Major, "", ExpectedResult = "2.0.0-1+1")]

        [TestCase(IncrementStrategy.None, "foo", ExpectedResult = "0.2.0-foo.1+1")]
        [TestCase(IncrementStrategy.Patch, "foo", ExpectedResult = "0.2.0-foo.1+1")]
        [TestCase(IncrementStrategy.Minor, "foo", ExpectedResult = "0.2.0-foo.1+1")]
        [TestCase(IncrementStrategy.Major, "foo", ExpectedResult = "2.0.0-foo.1+1")]

        [TestCase(IncrementStrategy.None, "bar", ExpectedResult = "0.2.0-bar")]
        [TestCase(IncrementStrategy.Patch, "bar", ExpectedResult = "0.2.0-bar")]
        [TestCase(IncrementStrategy.Minor, "bar", ExpectedResult = "0.2.0-bar")]
        [TestCase(IncrementStrategy.Major, "bar", ExpectedResult = "0.2.0-bar")]
        public string GetVersion(IncrementStrategy increment, string? label)
        {
            IGitVersionConfiguration trunkBased = TrunkBasedBuilder
                .WithBranch("main", _ => _.WithIncrement(increment).WithLabel(label))
                .Build();

            return fixture!.GetVersion(trunkBased).FullSemVer;
        }
    }
}
