using GitVersion.Configuration;
using GitVersion.Core.Tests;
using GitVersion.Core.Tests.IntegrationTests;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Mainline;

internal partial class MainlineScenariosWithAGitHubFlow
{
    [Parallelizable(ParallelScope.All)]
    public class GivenAMainBranchWithTwoCommitsWhenSecondCommitTaggedAsPreReleaseFoo
    {
        private EmptyRepositoryFixture? fixture;

        private static GitHubFlowConfigurationBuilder MainlineBuilder => GitHubFlowConfigurationBuilder.New
            .WithVersionStrategy(VersionStrategies.Mainline).WithLabel(null)
            .WithBranch("main", b => b.WithDeploymentMode(DeploymentMode.ManualDeployment));

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // B 58 minutes ago  (HEAD -> main) (tag 0.2.0-foo.4)
            // A 59 minutes ago

            fixture = new EmptyRepositoryFixture();

            fixture.MakeACommit("A");
            fixture.MakeACommit("B");
            fixture.ApplyTag("0.2.0-foo.4");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => fixture?.Dispose();

        [TestCase(IncrementStrategy.None, null, ExpectedResult = "0.2.0-foo.4")]
        [TestCase(IncrementStrategy.Patch, null, ExpectedResult = "0.2.0-foo.4")]
        [TestCase(IncrementStrategy.Minor, null, ExpectedResult = "0.2.0-foo.4")]
        [TestCase(IncrementStrategy.Major, null, ExpectedResult = "0.2.0-foo.4")]

        [TestCase(IncrementStrategy.None, "", ExpectedResult = "0.2.0-1+1")]
        [TestCase(IncrementStrategy.Patch, "", ExpectedResult = "0.2.0-1+1")]
        [TestCase(IncrementStrategy.Minor, "", ExpectedResult = "0.2.0-1+1")]
        [TestCase(IncrementStrategy.Major, "", ExpectedResult = "2.0.0-1+1")]

        [TestCase(IncrementStrategy.None, "foo", ExpectedResult = "0.2.0-foo.4")]
        [TestCase(IncrementStrategy.Patch, "foo", ExpectedResult = "0.2.0-foo.4")]
        [TestCase(IncrementStrategy.Minor, "foo", ExpectedResult = "0.2.0-foo.4")]
        [TestCase(IncrementStrategy.Major, "foo", ExpectedResult = "0.2.0-foo.4")]

        [TestCase(IncrementStrategy.None, "bar", ExpectedResult = "0.2.0-bar.1+1")]
        [TestCase(IncrementStrategy.Patch, "bar", ExpectedResult = "0.2.0-bar.1+1")]
        [TestCase(IncrementStrategy.Minor, "bar", ExpectedResult = "0.2.0-bar.1+1")]
        [TestCase(IncrementStrategy.Major, "bar", ExpectedResult = "2.0.0-bar.1+1")]
        public string GetVersion(IncrementStrategy increment, string? label)
        {
            var mainline = MainlineBuilder
                .WithBranch("main", b => b.WithIncrement(increment).WithLabel(label))
                .Build();

            return fixture!.GetVersion(mainline).FullSemVer;
        }
    }
}
