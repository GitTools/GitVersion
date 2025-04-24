using GitVersion.Configuration;
using GitVersion.Core.Tests;
using GitVersion.Core.Tests.IntegrationTests;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Mainline;

internal partial class MainlineScenariosWithAGitHubFlow
{
    [Parallelizable(ParallelScope.All)]
    public class GivenAFeatureBranchWithThreeCommitsBranchedFromMainWhen
    {
        private EmptyRepositoryFixture? fixture;

        private static GitHubFlowConfigurationBuilder MainlineBuilder => GitHubFlowConfigurationBuilder.New
            .WithVersionStrategy(VersionStrategies.Mainline).WithLabel(null)
            .WithBranch("main", b => b.WithDeploymentMode(DeploymentMode.ManualDeployment))
            .WithBranch("feature", b => b
                .WithDeploymentMode(DeploymentMode.ManualDeployment).WithPreventIncrementWhenCurrentCommitTagged(true)
            );

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // D 45 minutes ago  (HEAD -> feature/foo)
            // C 46 minutes ago
            // B 47 minutes ago
            // A 51 minutes ago  (main)

            fixture = new EmptyRepositoryFixture();

            fixture.MakeACommit("A");
            fixture.BranchTo("feature/foo");
            fixture.MakeACommit("B");
            fixture.MakeACommit("C");
            fixture.MakeACommit("D");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => fixture?.Dispose();

        [TestCase(IncrementStrategy.None, IncrementStrategy.None, null, ExpectedResult = "0.0.0-2+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, null, ExpectedResult = "0.0.1-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, null, ExpectedResult = "0.1.0-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, null, ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.None, "", ExpectedResult = "0.0.0-2+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, "", ExpectedResult = "0.0.1-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, "", ExpectedResult = "0.1.0-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, "", ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.None, "foo", ExpectedResult = "0.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, "foo", ExpectedResult = "0.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, "foo", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, "foo", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.None, "bar", ExpectedResult = "0.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, "bar", ExpectedResult = "0.0.1-bar.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, "bar", ExpectedResult = "0.1.0-bar.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.None, "{BranchName}", ExpectedResult = "0.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, "{BranchName}", ExpectedResult = "0.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, "{BranchName}", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, "{BranchName}", ExpectedResult = "1.0.0-foo.1+3")]

        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, null, ExpectedResult = "0.0.1-2+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, null, ExpectedResult = "0.0.2-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, null, ExpectedResult = "0.1.0-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, null, ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, "", ExpectedResult = "0.0.1-2+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, "", ExpectedResult = "0.0.2-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, "", ExpectedResult = "0.1.0-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, "", ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, "foo", ExpectedResult = "0.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, "foo", ExpectedResult = "0.0.2-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, "foo", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, "foo", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, "bar", ExpectedResult = "0.0.1-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, "bar", ExpectedResult = "0.0.2-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, "bar", ExpectedResult = "0.1.0-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, "{BranchName}", ExpectedResult = "0.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, "{BranchName}", ExpectedResult = "0.0.2-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, "{BranchName}", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, "{BranchName}", ExpectedResult = "1.0.0-foo.1+3")]

        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, null, ExpectedResult = "0.1.0-2+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, null, ExpectedResult = "0.1.1-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, null, ExpectedResult = "0.2.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, null, ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, "", ExpectedResult = "0.1.0-2+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, "", ExpectedResult = "0.1.1-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, "", ExpectedResult = "0.2.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, "", ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, "foo", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, "foo", ExpectedResult = "0.1.1-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, "foo", ExpectedResult = "0.2.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, "foo", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, "bar", ExpectedResult = "0.1.0-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, "bar", ExpectedResult = "0.1.1-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, "bar", ExpectedResult = "0.2.0-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, "{BranchName}", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, "{BranchName}", ExpectedResult = "0.1.1-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, "{BranchName}", ExpectedResult = "0.2.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, "{BranchName}", ExpectedResult = "1.0.0-foo.1+3")]

        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, null, ExpectedResult = "1.0.0-2+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, null, ExpectedResult = "1.0.1-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, null, ExpectedResult = "1.1.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, null, ExpectedResult = "2.0.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, "", ExpectedResult = "1.0.0-2+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, "", ExpectedResult = "1.0.1-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, "", ExpectedResult = "1.1.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, "", ExpectedResult = "2.0.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, "foo", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, "foo", ExpectedResult = "1.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, "foo", ExpectedResult = "1.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, "foo", ExpectedResult = "2.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, "bar", ExpectedResult = "1.0.1-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, "bar", ExpectedResult = "1.1.0-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, "bar", ExpectedResult = "2.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, "{BranchName}", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, "{BranchName}", ExpectedResult = "1.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, "{BranchName}", ExpectedResult = "1.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, "{BranchName}", ExpectedResult = "2.0.0-foo.1+3")]
        public string GetVersionWithNoLabelOnMain(IncrementStrategy incrementOnMain, IncrementStrategy increment, string? label)
        {
            var mainline = MainlineBuilder
                .WithBranch("main", b => b.WithIncrement(incrementOnMain).WithLabel(null))
                .WithBranch("feature", b => b.WithIncrement(increment).WithLabel(label))
                .Build();

            return fixture!.GetVersion(mainline).FullSemVer;
        }

        [TestCase(IncrementStrategy.None, IncrementStrategy.None, null, ExpectedResult = "0.0.0-2+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, null, ExpectedResult = "0.0.1-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, null, ExpectedResult = "0.1.0-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, null, ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.None, "", ExpectedResult = "0.0.0-2+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, "", ExpectedResult = "0.0.1-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, "", ExpectedResult = "0.1.0-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, "", ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.None, "foo", ExpectedResult = "0.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, "foo", ExpectedResult = "0.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, "foo", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, "foo", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.None, "bar", ExpectedResult = "0.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, "bar", ExpectedResult = "0.0.1-bar.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, "bar", ExpectedResult = "0.1.0-bar.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.None, "{BranchName}", ExpectedResult = "0.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, "{BranchName}", ExpectedResult = "0.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, "{BranchName}", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, "{BranchName}", ExpectedResult = "1.0.0-foo.1+3")]

        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, null, ExpectedResult = "0.0.1-2+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, null, ExpectedResult = "0.0.2-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, null, ExpectedResult = "0.1.0-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, null, ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, "", ExpectedResult = "0.0.1-2+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, "", ExpectedResult = "0.0.2-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, "", ExpectedResult = "0.1.0-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, "", ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, "foo", ExpectedResult = "0.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, "foo", ExpectedResult = "0.0.2-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, "foo", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, "foo", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, "bar", ExpectedResult = "0.0.1-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, "bar", ExpectedResult = "0.0.2-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, "bar", ExpectedResult = "0.1.0-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, "{BranchName}", ExpectedResult = "0.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, "{BranchName}", ExpectedResult = "0.0.2-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, "{BranchName}", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, "{BranchName}", ExpectedResult = "1.0.0-foo.1+3")]

        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, null, ExpectedResult = "0.1.0-2+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, null, ExpectedResult = "0.1.1-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, null, ExpectedResult = "0.2.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, null, ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, "", ExpectedResult = "0.1.0-2+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, "", ExpectedResult = "0.1.1-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, "", ExpectedResult = "0.2.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, "", ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, "foo", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, "foo", ExpectedResult = "0.1.1-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, "foo", ExpectedResult = "0.2.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, "foo", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, "bar", ExpectedResult = "0.1.0-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, "bar", ExpectedResult = "0.1.1-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, "bar", ExpectedResult = "0.2.0-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, "{BranchName}", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, "{BranchName}", ExpectedResult = "0.1.1-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, "{BranchName}", ExpectedResult = "0.2.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, "{BranchName}", ExpectedResult = "1.0.0-foo.1+3")]

        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, null, ExpectedResult = "1.0.0-2+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, null, ExpectedResult = "1.0.1-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, null, ExpectedResult = "1.1.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, null, ExpectedResult = "2.0.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, "", ExpectedResult = "1.0.0-2+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, "", ExpectedResult = "1.0.1-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, "", ExpectedResult = "1.1.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, "", ExpectedResult = "2.0.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, "foo", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, "foo", ExpectedResult = "1.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, "foo", ExpectedResult = "1.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, "foo", ExpectedResult = "2.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, "bar", ExpectedResult = "1.0.1-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, "bar", ExpectedResult = "1.1.0-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, "bar", ExpectedResult = "2.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, "{BranchName}", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, "{BranchName}", ExpectedResult = "1.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, "{BranchName}", ExpectedResult = "1.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, "{BranchName}", ExpectedResult = "2.0.0-foo.1+3")]
        public string GetVersionWithEmptyLabelOnMain(IncrementStrategy incrementOnMain, IncrementStrategy increment, string? label)
        {
            var mainline = MainlineBuilder
                .WithBranch("main", b => b.WithIncrement(incrementOnMain).WithLabel(string.Empty))
                .WithBranch("feature", b => b.WithIncrement(increment).WithLabel(label))
                .Build();

            return fixture!.GetVersion(mainline).FullSemVer;
        }

        [TestCase(IncrementStrategy.None, IncrementStrategy.None, null, ExpectedResult = "0.0.0-foo.2+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, null, ExpectedResult = "0.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, null, ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, null, ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.None, "", ExpectedResult = "0.0.0-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, "", ExpectedResult = "0.0.1-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, "", ExpectedResult = "0.1.0-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, "", ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.None, "foo", ExpectedResult = "0.0.0-foo.2+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, "foo", ExpectedResult = "0.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, "foo", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, "foo", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.None, "bar", ExpectedResult = "0.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, "bar", ExpectedResult = "0.0.1-bar.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, "bar", ExpectedResult = "0.1.0-bar.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.None, "{BranchName}", ExpectedResult = "0.0.0-foo.2+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, "{BranchName}", ExpectedResult = "0.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, "{BranchName}", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, "{BranchName}", ExpectedResult = "1.0.0-foo.1+3")]

        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, null, ExpectedResult = "0.0.1-foo.2+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, null, ExpectedResult = "0.0.2-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, null, ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, null, ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, "", ExpectedResult = "0.0.1-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, "", ExpectedResult = "0.0.2-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, "", ExpectedResult = "0.1.0-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, "", ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, "foo", ExpectedResult = "0.0.1-foo.2+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, "foo", ExpectedResult = "0.0.2-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, "foo", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, "foo", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, "bar", ExpectedResult = "0.0.1-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, "bar", ExpectedResult = "0.0.2-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, "bar", ExpectedResult = "0.1.0-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, "{BranchName}", ExpectedResult = "0.0.1-foo.2+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, "{BranchName}", ExpectedResult = "0.0.2-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, "{BranchName}", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, "{BranchName}", ExpectedResult = "1.0.0-foo.1+3")]

        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, null, ExpectedResult = "0.1.0-foo.2+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, null, ExpectedResult = "0.1.1-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, null, ExpectedResult = "0.2.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, null, ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, "", ExpectedResult = "0.1.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, "", ExpectedResult = "0.1.1-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, "", ExpectedResult = "0.2.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, "", ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, "foo", ExpectedResult = "0.1.0-foo.2+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, "foo", ExpectedResult = "0.1.1-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, "foo", ExpectedResult = "0.2.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, "foo", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, "bar", ExpectedResult = "0.1.0-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, "bar", ExpectedResult = "0.1.1-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, "bar", ExpectedResult = "0.2.0-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, "{BranchName}", ExpectedResult = "0.1.0-foo.2+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, "{BranchName}", ExpectedResult = "0.1.1-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, "{BranchName}", ExpectedResult = "0.2.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, "{BranchName}", ExpectedResult = "1.0.0-foo.1+3")]

        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, null, ExpectedResult = "1.0.0-foo.2+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, null, ExpectedResult = "1.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, null, ExpectedResult = "1.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, null, ExpectedResult = "2.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, "", ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, "", ExpectedResult = "1.0.1-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, "", ExpectedResult = "1.1.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, "", ExpectedResult = "2.0.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, "foo", ExpectedResult = "1.0.0-foo.2+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, "foo", ExpectedResult = "1.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, "foo", ExpectedResult = "1.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, "foo", ExpectedResult = "2.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, "bar", ExpectedResult = "1.0.1-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, "bar", ExpectedResult = "1.1.0-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, "bar", ExpectedResult = "2.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, "{BranchName}", ExpectedResult = "1.0.0-foo.2+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, "{BranchName}", ExpectedResult = "1.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, "{BranchName}", ExpectedResult = "1.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, "{BranchName}", ExpectedResult = "2.0.0-foo.1+3")]
        public string GetVersionWithLabelFooOnMain(IncrementStrategy incrementOnMain, IncrementStrategy increment, string? label)
        {
            var mainline = MainlineBuilder
                .WithBranch("main", b => b.WithIncrement(incrementOnMain).WithLabel("foo"))
                .WithBranch("feature", b => b.WithIncrement(increment).WithLabel(label))
                .Build();

            return fixture!.GetVersion(mainline).FullSemVer;
        }
    }
}
