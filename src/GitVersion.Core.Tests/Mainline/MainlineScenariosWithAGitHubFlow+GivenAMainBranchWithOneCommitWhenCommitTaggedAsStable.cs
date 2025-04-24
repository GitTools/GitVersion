using GitVersion.Configuration;
using GitVersion.Core.Tests;
using GitVersion.Core.Tests.IntegrationTests;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Mainline;

internal partial class MainlineScenariosWithAGitHubFlow
{
    [Parallelizable(ParallelScope.All)]
    public class GivenAMainBranchWithOneCommitWhenCommitTaggedAsStable
    {
        private EmptyRepositoryFixture? fixture;

        private static GitHubFlowConfigurationBuilder MainlineBuilder => GitHubFlowConfigurationBuilder.New
            .WithVersionStrategy(VersionStrategies.Mainline).WithLabel(null)
            .WithBranch("main", b => b.WithDeploymentMode(DeploymentMode.ManualDeployment));

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // A 59 minutes ago  (HEAD -> main) (tag 0.0.0)

            fixture = new EmptyRepositoryFixture();

            fixture.MakeACommit("A");
            fixture.ApplyTag("0.0.0");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => fixture?.Dispose();

        [TestCase(IncrementStrategy.None, null, ExpectedResult = "0.0.0")]
        [TestCase(IncrementStrategy.Patch, null, ExpectedResult = "0.0.0")]
        [TestCase(IncrementStrategy.Minor, null, ExpectedResult = "0.0.0")]
        [TestCase(IncrementStrategy.Major, null, ExpectedResult = "0.0.0")]

        [TestCase(IncrementStrategy.None, "", ExpectedResult = "0.0.0")]
        [TestCase(IncrementStrategy.Patch, "", ExpectedResult = "0.0.0")]
        [TestCase(IncrementStrategy.Minor, "", ExpectedResult = "0.0.0")]
        [TestCase(IncrementStrategy.Major, "", ExpectedResult = "0.0.0")]

        [TestCase(IncrementStrategy.None, "foo", ExpectedResult = "0.0.0")]
        [TestCase(IncrementStrategy.Patch, "foo", ExpectedResult = "0.0.0")]
        [TestCase(IncrementStrategy.Minor, "foo", ExpectedResult = "0.0.0")]
        [TestCase(IncrementStrategy.Major, "foo", ExpectedResult = "0.0.0")]

        [TestCase(IncrementStrategy.None, "bar", ExpectedResult = "0.0.0")]
        [TestCase(IncrementStrategy.Patch, "bar", ExpectedResult = "0.0.0")]
        [TestCase(IncrementStrategy.Minor, "bar", ExpectedResult = "0.0.0")]
        [TestCase(IncrementStrategy.Major, "bar", ExpectedResult = "0.0.0")]
        public string GetVersion(IncrementStrategy increment, string? label)
        {
            var mainline = MainlineBuilder
                .WithBranch("main", b => b.WithIncrement(increment).WithLabel(label))
                .Build();

            return fixture!.GetVersion(mainline).FullSemVer;
        }
    }
}
