using GitVersion.Configuration;
using GitVersion.Core.Tests;
using GitVersion.Core.Tests.IntegrationTests;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.TrunkBased;

internal partial class TrunkBasedScenariosWithAGitHubFlow
{
    [Parallelizable(ParallelScope.All)]
    public class GivenAFeatureBranchWithThreeCommitsWhen
    {
        private EmptyRepositoryFixture? fixture;

        private static GitHubFlowConfigurationBuilder TrunkBasedBuilder => GitHubFlowConfigurationBuilder.New
            .WithVersioningMode(VersioningMode.TrunkBased).WithLabel(null)
            .WithBranch("feature", _ => _.WithVersioningMode(VersioningMode.ManualDeployment).WithIsMainline(false));

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // C 57 minutes ago  (HEAD -> feature/foo)
            // B 58 minutes ago
            // A 59 minutes ago

            fixture = new EmptyRepositoryFixture("feature/foo");

            fixture.MakeACommit("A");
            fixture.MakeACommit("B");
            fixture.MakeACommit("C");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => fixture?.Dispose();

        [TestCase(IncrementStrategy.None, null, ExpectedResult = "0.0.0-1+3")]
        [TestCase(IncrementStrategy.Patch, null, ExpectedResult = "0.0.1-1+3")]
        [TestCase(IncrementStrategy.Minor, null, ExpectedResult = "0.1.0-1+3")]
        [TestCase(IncrementStrategy.Major, null, ExpectedResult = "1.0.0-1+3")]

        [TestCase(IncrementStrategy.None, "", ExpectedResult = "0.0.0-1+3")]
        [TestCase(IncrementStrategy.Patch, "", ExpectedResult = "0.0.1-1+3")]
        [TestCase(IncrementStrategy.Minor, "", ExpectedResult = "0.1.0-1+3")]
        [TestCase(IncrementStrategy.Major, "", ExpectedResult = "1.0.0-1+3")]

        [TestCase(IncrementStrategy.None, "foo", ExpectedResult = "0.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, "foo", ExpectedResult = "0.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, "foo", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, "foo", ExpectedResult = "1.0.0-foo.1+3")]

        [TestCase(IncrementStrategy.None, "bar", ExpectedResult = "0.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, "bar", ExpectedResult = "0.0.1-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, "bar", ExpectedResult = "0.1.0-bar.1+3")]
        [TestCase(IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        public string GetVersion(IncrementStrategy increment, string? label)
        {
            IGitVersionConfiguration trunkBased = TrunkBasedBuilder
                .WithBranch("feature", _ => _.WithIncrement(increment).WithLabel(label))
                .Build();

            return fixture!.GetVersion(trunkBased).FullSemVer;
        }
    }
}
