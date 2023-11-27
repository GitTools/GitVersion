using GitVersion.Configuration;
using GitVersion.Core.Tests;
using GitVersion.Core.Tests.IntegrationTests;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.TrunkBased;

internal partial class TrunkBasedScenariosWithAGitHubFlow
{
    [Parallelizable(ParallelScope.All)]
    public class GivenAMainBranchWithThreeCommitsWhen
    {
        private EmptyRepositoryFixture? fixture;

        private static GitHubFlowConfigurationBuilder TrunkBasedBuilder => GitHubFlowConfigurationBuilder.New
            .WithVersioningMode(VersioningMode.TrunkBased).WithLabel(null)
            .WithBranch("main", _ => _.WithVersioningMode(VersioningMode.ManualDeployment));

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // C 57 minutes agov (HEAD -> main)
            // B 58 minutes ago
            // A 59 minutes ago

            fixture = new EmptyRepositoryFixture("main");

            fixture.MakeACommit("A");
            fixture.MakeACommit("B");
            fixture.MakeACommit("C");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => fixture?.Dispose();

        [TestCase(IncrementStrategy.None, null, ExpectedResult = "0.0.0-3+1")]
        [TestCase(IncrementStrategy.Patch, null, ExpectedResult = "0.0.3-1+1")]
        [TestCase(IncrementStrategy.Minor, null, ExpectedResult = "0.3.0-1+1")]
        [TestCase(IncrementStrategy.Major, null, ExpectedResult = "3.0.0-1+1")]

        [TestCase(IncrementStrategy.None, "", ExpectedResult = "0.0.0-3+1")]
        [TestCase(IncrementStrategy.Patch, "", ExpectedResult = "0.0.3-1+1")]
        [TestCase(IncrementStrategy.Minor, "", ExpectedResult = "0.3.0-1+1")]
        [TestCase(IncrementStrategy.Major, "", ExpectedResult = "3.0.0-1+1")]

        [TestCase(IncrementStrategy.None, "foo", ExpectedResult = "0.0.0-foo.3+1")]
        [TestCase(IncrementStrategy.Patch, "foo", ExpectedResult = "0.0.3-foo.1+1")]
        [TestCase(IncrementStrategy.Minor, "foo", ExpectedResult = "0.3.0-foo.1+1")]
        [TestCase(IncrementStrategy.Major, "foo", ExpectedResult = "3.0.0-foo.1+1")]

        [TestCase(IncrementStrategy.None, "bar", ExpectedResult = "0.0.0-bar.3+1")]
        [TestCase(IncrementStrategy.Patch, "bar", ExpectedResult = "0.0.3-bar.1+1")]
        [TestCase(IncrementStrategy.Minor, "bar", ExpectedResult = "0.3.0-bar.1+1")]
        [TestCase(IncrementStrategy.Major, "bar", ExpectedResult = "3.0.0-bar.1+1")]
        public string GetVersion(IncrementStrategy increment, string? label)
        {
            IGitVersionConfiguration trunkBased = TrunkBasedBuilder
                .WithBranch("main", _ => _.WithIncrement(increment).WithLabel(label))
                .Build();

            return fixture!.GetVersion(trunkBased).FullSemVer;
        }
    }
}
