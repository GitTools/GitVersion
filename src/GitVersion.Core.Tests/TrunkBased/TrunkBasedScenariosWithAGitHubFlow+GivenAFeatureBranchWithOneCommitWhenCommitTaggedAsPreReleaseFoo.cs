using GitVersion.Configuration;
using GitVersion.Core.Tests;
using GitVersion.Core.Tests.IntegrationTests;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.TrunkBased;

internal partial class TrunkBasedScenariosWithAGitHubFlow
{
    [Parallelizable(ParallelScope.All)]
    public class GivenAFeatureBranchWithOneCommitWhenCommitTaggedAsPreReleaseFoo
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
            // A 59 minutes ago (HEAD -> feature/foo) (tag 0.0.0-foo.4)

            fixture = new EmptyRepositoryFixture("feature/foo");

            fixture.MakeACommit("A");
            fixture.ApplyTag("0.0.0-foo.4");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => fixture?.Dispose();

        [TestCase(IncrementStrategy.None, null, ExpectedResult = "0.0.0-foo.4")]
        [TestCase(IncrementStrategy.Patch, null, ExpectedResult = "0.0.0-foo.4")]
        [TestCase(IncrementStrategy.Minor, null, ExpectedResult = "0.0.0-foo.4")]
        [TestCase(IncrementStrategy.Major, null, ExpectedResult = "0.0.0-foo.4")]

        [TestCase(IncrementStrategy.None, "", ExpectedResult = "0.0.0-1+1")]
        [TestCase(IncrementStrategy.Patch, "", ExpectedResult = "0.0.1-1+1")]
        [TestCase(IncrementStrategy.Minor, "", ExpectedResult = "0.1.0-1+1")]
        [TestCase(IncrementStrategy.Major, "", ExpectedResult = "1.0.0-1+1")]

        [TestCase(IncrementStrategy.None, "foo", ExpectedResult = "0.0.0-foo.4")]
        [TestCase(IncrementStrategy.Patch, "foo", ExpectedResult = "0.0.0-foo.4")]
        [TestCase(IncrementStrategy.Minor, "foo", ExpectedResult = "0.0.0-foo.4")]
        [TestCase(IncrementStrategy.Major, "foo", ExpectedResult = "0.0.0-foo.4")]

        [TestCase(IncrementStrategy.None, "bar", ExpectedResult = "0.0.0-bar.1+1")]
        [TestCase(IncrementStrategy.Patch, "bar", ExpectedResult = "0.0.1-bar.1+1")]
        [TestCase(IncrementStrategy.Minor, "bar", ExpectedResult = "0.1.0-bar.1+1")]
        [TestCase(IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+1")]
        public string GetVersion(IncrementStrategy increment, string? label)
        {
            IGitVersionConfiguration trunkBased = TrunkBasedBuilder
                .WithBranch("feature", _ => _.WithIncrement(increment).WithLabel(label))
                .Build();

            return fixture!.GetVersion(trunkBased).FullSemVer;
        }

        [TestCase(IncrementStrategy.None, null, ExpectedResult = "0.0.0-foo.5+0")]
        [TestCase(IncrementStrategy.Patch, null, ExpectedResult = "0.0.0-foo.5+0")]
        [TestCase(IncrementStrategy.Minor, null, ExpectedResult = "0.0.0-foo.5+0")]
        [TestCase(IncrementStrategy.Major, null, ExpectedResult = "0.0.0-foo.5+0")]

        [TestCase(IncrementStrategy.None, "", ExpectedResult = "0.0.0-1+1")]
        [TestCase(IncrementStrategy.Patch, "", ExpectedResult = "0.0.1-1+1")]
        [TestCase(IncrementStrategy.Minor, "", ExpectedResult = "0.1.0-1+1")]
        [TestCase(IncrementStrategy.Major, "", ExpectedResult = "1.0.0-1+1")]

        [TestCase(IncrementStrategy.None, "foo", ExpectedResult = "0.0.0-foo.5+0")]
        [TestCase(IncrementStrategy.Patch, "foo", ExpectedResult = "0.0.0-foo.5+0")]
        [TestCase(IncrementStrategy.Minor, "foo", ExpectedResult = "0.0.0-foo.5+0")]
        [TestCase(IncrementStrategy.Major, "foo", ExpectedResult = "0.0.0-foo.5+0")]

        [TestCase(IncrementStrategy.None, "bar", ExpectedResult = "0.0.0-bar.1+1")]
        [TestCase(IncrementStrategy.Patch, "bar", ExpectedResult = "0.0.1-bar.1+1")]
        [TestCase(IncrementStrategy.Minor, "bar", ExpectedResult = "0.1.0-bar.1+1")]
        [TestCase(IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+1")]
        public string GetVersionWithPreventIncrementWhenCurrentCommitTaggedFalse(IncrementStrategy increment, string? label)
        {
            IGitVersionConfiguration trunkBased = TrunkBasedBuilder
                .WithBranch("feature", _ => _.WithIncrement(increment).WithLabel(label).WithPreventIncrementWhenCurrentCommitTagged(false))
                .Build();

            return fixture!.GetVersion(trunkBased).FullSemVer;
        }
    }
}
