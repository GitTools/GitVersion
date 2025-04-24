using GitVersion.Configuration;
using GitVersion.Core.Tests;
using GitVersion.Core.Tests.IntegrationTests;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Mainline;

internal partial class MainlineScenariosWithAGitFlow
{
    [Parallelizable(ParallelScope.All)]
    public class GivenAFeatureBranchWithAMergeCommitFromMainMergedBackToMainWhen
    {
        private EmptyRepositoryFixture? fixture;

        private static GitHubFlowConfigurationBuilder MainlineBuilder => GitHubFlowConfigurationBuilder.New.WithLabel(null)
            .WithVersionStrategy(VersionStrategies.Mainline)
            .WithBranch("main", b => b.WithDeploymentMode(DeploymentMode.ManualDeployment))
            .WithBranch("feature", b => b
                .WithDeploymentMode(DeploymentMode.ManualDeployment).WithPreventIncrementWhenCurrentCommitTagged(true)
            );

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // *  52 minutes ago  (HEAD -> main)
            // |\
            // | *  53 minutes ago  (feature/foo)
            // | |\
            // | |/
            // |/|
            // C |  54 minutes ago
            // | B  56 minutes ago
            // |/
            // A  58 minutes ago

            fixture = new EmptyRepositoryFixture();

            fixture.MakeACommit("A");
            fixture.BranchTo("feature/foo");
            fixture.MakeACommit("B");
            fixture.Checkout("main");
            fixture.MakeACommit("C");
            fixture.Checkout("main");
            fixture.MergeTo("feature/foo");
            fixture.MergeTo("main", removeBranchAfterMerging: true);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => fixture?.Dispose();

        [TestCase(IncrementStrategy.None, IncrementStrategy.None, null, ExpectedResult = "0.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, null, ExpectedResult = "0.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, null, ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, null, ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Inherit, null, ExpectedResult = "0.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.None, "", ExpectedResult = "0.0.0-3+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, "", ExpectedResult = "0.0.1-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, "", ExpectedResult = "0.1.0-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, "", ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Inherit, "", ExpectedResult = "0.0.0-3+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.None, "foo", ExpectedResult = "0.0.0-foo.3+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, "foo", ExpectedResult = "0.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, "foo", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, "foo", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Inherit, "foo", ExpectedResult = "0.0.0-foo.3+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.None, "bar", ExpectedResult = "0.0.0-bar.3+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, "bar", ExpectedResult = "0.0.1-bar.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, "bar", ExpectedResult = "0.1.0-bar.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Inherit, "bar", ExpectedResult = "0.0.0-bar.3+3")]

        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, null, ExpectedResult = "0.0.2-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, null, ExpectedResult = "0.0.3-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, null, ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, null, ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Inherit, null, ExpectedResult = "0.0.3-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, "", ExpectedResult = "0.0.2-2+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, "", ExpectedResult = "0.0.3-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, "", ExpectedResult = "0.1.0-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, "", ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Inherit, "", ExpectedResult = "0.0.3-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, "foo", ExpectedResult = "0.0.2-foo.2+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, "foo", ExpectedResult = "0.0.3-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, "foo", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, "foo", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Inherit, "foo", ExpectedResult = "0.0.3-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, "bar", ExpectedResult = "0.0.2-bar.2+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, "bar", ExpectedResult = "0.0.3-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, "bar", ExpectedResult = "0.1.0-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Inherit, "bar", ExpectedResult = "0.0.3-bar.1+3")]

        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, null, ExpectedResult = "0.2.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, null, ExpectedResult = "0.2.1-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, null, ExpectedResult = "0.3.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, null, ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Inherit, null, ExpectedResult = "0.3.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, "", ExpectedResult = "0.2.0-2+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, "", ExpectedResult = "0.2.1-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, "", ExpectedResult = "0.3.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, "", ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Inherit, "", ExpectedResult = "0.3.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, "foo", ExpectedResult = "0.2.0-foo.2+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, "foo", ExpectedResult = "0.2.1-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, "foo", ExpectedResult = "0.3.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, "foo", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Inherit, "foo", ExpectedResult = "0.3.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, "bar", ExpectedResult = "0.2.0-bar.2+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, "bar", ExpectedResult = "0.2.1-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, "bar", ExpectedResult = "0.3.0-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Inherit, "bar", ExpectedResult = "0.3.0-bar.1+3")]

        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, null, ExpectedResult = "2.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, null, ExpectedResult = "2.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, null, ExpectedResult = "2.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, null, ExpectedResult = "3.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Inherit, null, ExpectedResult = "3.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, "", ExpectedResult = "2.0.0-2+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, "", ExpectedResult = "2.0.1-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, "", ExpectedResult = "2.1.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, "", ExpectedResult = "3.0.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Inherit, "", ExpectedResult = "3.0.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, "foo", ExpectedResult = "2.0.0-foo.2+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, "foo", ExpectedResult = "2.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, "foo", ExpectedResult = "2.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, "foo", ExpectedResult = "3.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Inherit, "foo", ExpectedResult = "3.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, "bar", ExpectedResult = "2.0.0-bar.2+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, "bar", ExpectedResult = "2.0.1-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, "bar", ExpectedResult = "2.1.0-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, "bar", ExpectedResult = "3.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Inherit, "bar", ExpectedResult = "3.0.0-bar.1+3")]
        public string GetVersion(IncrementStrategy increment, IncrementStrategy incrementOnFeature, string? label)
        {
            var mainline = MainlineBuilder
                .WithBranch("main", b => b.WithIncrement(increment).WithLabel(label))
                .WithBranch("feature", b => b.WithIncrement(incrementOnFeature))
                .Build();

            return fixture!.GetVersion(mainline).FullSemVer;
        }

        [TestCase(IncrementStrategy.None, IncrementStrategy.None, null, ExpectedResult = "0.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, null, ExpectedResult = "0.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, null, ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, null, ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Inherit, null, ExpectedResult = "0.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.None, "", ExpectedResult = "0.0.0-3+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, "", ExpectedResult = "0.0.1-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, "", ExpectedResult = "0.1.0-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, "", ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Inherit, "", ExpectedResult = "0.0.0-3+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.None, "foo", ExpectedResult = "0.0.0-foo.3+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, "foo", ExpectedResult = "0.0.1-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, "foo", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, "foo", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Inherit, "foo", ExpectedResult = "0.0.0-foo.3+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.None, "bar", ExpectedResult = "0.0.0-bar.3+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, "bar", ExpectedResult = "0.0.1-bar.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, "bar", ExpectedResult = "0.1.0-bar.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Inherit, "bar", ExpectedResult = "0.0.0-bar.3+3")]

        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, null, ExpectedResult = "0.0.3-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, null, ExpectedResult = "0.0.3-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, null, ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, null, ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Inherit, null, ExpectedResult = "0.0.3-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, "", ExpectedResult = "0.0.3-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, "", ExpectedResult = "0.0.3-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, "", ExpectedResult = "0.1.0-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, "", ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Inherit, "", ExpectedResult = "0.0.3-1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, "foo", ExpectedResult = "0.0.3-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, "foo", ExpectedResult = "0.0.3-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, "foo", ExpectedResult = "0.1.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, "foo", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Inherit, "foo", ExpectedResult = "0.0.3-foo.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, "bar", ExpectedResult = "0.0.3-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, "bar", ExpectedResult = "0.0.3-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, "bar", ExpectedResult = "0.1.0-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Inherit, "bar", ExpectedResult = "0.0.3-bar.1+3")]

        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, null, ExpectedResult = "0.3.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, null, ExpectedResult = "0.3.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, null, ExpectedResult = "0.3.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, null, ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Inherit, null, ExpectedResult = "0.3.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, "", ExpectedResult = "0.3.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, "", ExpectedResult = "0.3.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, "", ExpectedResult = "0.3.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, "", ExpectedResult = "1.0.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Inherit, "", ExpectedResult = "0.3.0-1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, "foo", ExpectedResult = "0.3.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, "foo", ExpectedResult = "0.3.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, "foo", ExpectedResult = "0.3.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, "foo", ExpectedResult = "1.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Inherit, "foo", ExpectedResult = "0.3.0-foo.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, "bar", ExpectedResult = "0.3.0-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, "bar", ExpectedResult = "0.3.0-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, "bar", ExpectedResult = "0.3.0-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, "bar", ExpectedResult = "1.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Inherit, "bar", ExpectedResult = "0.3.0-bar.1+3")]

        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, null, ExpectedResult = "3.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, null, ExpectedResult = "3.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, null, ExpectedResult = "3.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, null, ExpectedResult = "3.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Inherit, null, ExpectedResult = "3.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, "", ExpectedResult = "3.0.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, "", ExpectedResult = "3.0.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, "", ExpectedResult = "3.0.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, "", ExpectedResult = "3.0.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Inherit, "", ExpectedResult = "3.0.0-1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, "foo", ExpectedResult = "3.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, "foo", ExpectedResult = "3.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, "foo", ExpectedResult = "3.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, "foo", ExpectedResult = "3.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Inherit, "foo", ExpectedResult = "3.0.0-foo.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, "bar", ExpectedResult = "3.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, "bar", ExpectedResult = "3.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, "bar", ExpectedResult = "3.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, "bar", ExpectedResult = "3.0.0-bar.1+3")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Inherit, "bar", ExpectedResult = "3.0.0-bar.1+3")]
        public string GetVersionWithPreventIncrementOfMergedBranchVersionFalseOnMain(
            IncrementStrategy increment, IncrementStrategy incrementOnFeature, string? label)
        {
            var mainline = MainlineBuilder
                .WithBranch("main", b => b.WithIncrement(increment).WithLabel(label).WithPreventIncrementOfMergedBranch(false))
                .WithBranch("feature", b => b.WithIncrement(incrementOnFeature))
                .Build();

            return fixture!.GetVersion(mainline).FullSemVer;
        }
    }
}
