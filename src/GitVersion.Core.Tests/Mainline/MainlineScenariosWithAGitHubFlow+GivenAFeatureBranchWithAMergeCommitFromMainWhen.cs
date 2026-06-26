using GitVersion.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Tests.Mainline;

internal partial class MainlineScenariosWithAGitHubFlow
{
    [Parallelizable(ParallelScope.All)]
    public class GivenAFeatureBranchWithAMergeCommitFromMainWhen
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
            // *  53 minutes ago  (HEAD -> feature/foo)
            // |\
            // | B  54 minutes ago
            // C |  56 minutes ago  (main)
            // |/
            // A  58 minutes ago

            this.fixture = new EmptyRepositoryFixture();

            this.fixture.MakeACommit("A");
            this.fixture.BranchTo("feature/foo");
            this.fixture.MakeACommit("B");
            this.fixture.Checkout("main");
            this.fixture.MakeACommit("C");
            this.fixture.MergeTo("feature/foo");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => this.fixture?.Dispose();

        [TestCase(IncrementStrategy.None, IncrementStrategy.None, ExpectedResult = "0.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, ExpectedResult = "0.0.1-foo.1+2")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, ExpectedResult = "0.1.0-foo.1+2")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, ExpectedResult = "1.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Inherit, ExpectedResult = "0.0.0-foo.1+2")]

        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, ExpectedResult = "0.0.2-foo.1+2")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, ExpectedResult = "0.0.3-foo.1+2")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, ExpectedResult = "0.1.0-foo.1+2")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, ExpectedResult = "1.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Inherit, ExpectedResult = "0.0.3-foo.1+2")]

        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, ExpectedResult = "0.2.0-foo.1+2")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, ExpectedResult = "0.2.1-foo.1+2")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, ExpectedResult = "0.3.0-foo.1+2")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, ExpectedResult = "1.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Inherit, ExpectedResult = "0.3.0-foo.1+2")]

        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, ExpectedResult = "2.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, ExpectedResult = "2.0.1-foo.1+2")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, ExpectedResult = "2.1.0-foo.1+2")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, ExpectedResult = "3.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Inherit, ExpectedResult = "3.0.0-foo.1+2")]
        public string GetVersionWithNoLabelOnMain(IncrementStrategy incrementOnMain, IncrementStrategy increment)
        {
            var mainline = MainlineBuilder
                .WithBranch("main", b => b.WithIncrement(incrementOnMain).WithLabel(null))
                .WithBranch("feature", b => b.WithIncrement(increment))
                .Build();

            return this.fixture!.GetVersion(mainline).FullSemVer;
        }

        [TestCase(IncrementStrategy.None, IncrementStrategy.None, ExpectedResult = "0.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, ExpectedResult = "0.0.1-foo.1+2")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, ExpectedResult = "0.1.0-foo.1+2")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, ExpectedResult = "1.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Inherit, ExpectedResult = "0.0.0-foo.1+2")]

        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, ExpectedResult = "0.0.2-foo.1+2")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, ExpectedResult = "0.0.3-foo.1+2")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, ExpectedResult = "0.1.0-foo.1+2")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, ExpectedResult = "1.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Inherit, ExpectedResult = "0.0.3-foo.1+2")]

        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, ExpectedResult = "0.2.0-foo.1+2")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, ExpectedResult = "0.2.1-foo.1+2")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, ExpectedResult = "0.3.0-foo.1+2")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, ExpectedResult = "1.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Inherit, ExpectedResult = "0.3.0-foo.1+2")]

        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, ExpectedResult = "2.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, ExpectedResult = "2.0.1-foo.1+2")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, ExpectedResult = "2.1.0-foo.1+2")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, ExpectedResult = "3.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Inherit, ExpectedResult = "3.0.0-foo.1+2")]
        public string GetVersionWithEmptyLabelOnMain(IncrementStrategy incrementOnMain, IncrementStrategy increment)
        {
            var mainline = MainlineBuilder
                .WithBranch("main", b => b.WithIncrement(incrementOnMain).WithLabel(string.Empty))
                .WithBranch("feature", b => b.WithIncrement(increment))
                .Build();

            return this.fixture!.GetVersion(mainline).FullSemVer;
        }

        [TestCase(IncrementStrategy.None, IncrementStrategy.None, ExpectedResult = "0.0.0-foo.3+2")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, ExpectedResult = "0.0.1-foo.1+2")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, ExpectedResult = "0.1.0-foo.1+2")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, ExpectedResult = "1.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Inherit, ExpectedResult = "0.0.0-foo.3+2")]

        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, ExpectedResult = "0.0.2-foo.2+2")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, ExpectedResult = "0.0.3-foo.1+2")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, ExpectedResult = "0.1.0-foo.1+2")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, ExpectedResult = "1.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Inherit, ExpectedResult = "0.0.3-foo.1+2")]

        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, ExpectedResult = "0.2.0-foo.2+2")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, ExpectedResult = "0.2.1-foo.1+2")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, ExpectedResult = "0.3.0-foo.1+2")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, ExpectedResult = "1.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Inherit, ExpectedResult = "0.3.0-foo.1+2")]

        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, ExpectedResult = "2.0.0-foo.2+2")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, ExpectedResult = "2.0.1-foo.1+2")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, ExpectedResult = "2.1.0-foo.1+2")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, ExpectedResult = "3.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Inherit, ExpectedResult = "3.0.0-foo.1+2")]
        public string GetVersionWithLabelFooOnMain(IncrementStrategy incrementOnMain, IncrementStrategy increment)
        {
            var mainline = MainlineBuilder
                .WithBranch("main", b => b.WithIncrement(incrementOnMain).WithLabel("foo"))
                .WithBranch("feature", b => b.WithIncrement(increment))
                .Build();

            return this.fixture!.GetVersion(mainline).FullSemVer;
        }

        [TestCase(IncrementStrategy.None, IncrementStrategy.None, ExpectedResult = "0.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, ExpectedResult = "0.0.1-foo.1+2")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, ExpectedResult = "0.1.0-foo.1+2")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, ExpectedResult = "1.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Inherit, ExpectedResult = "0.0.0-foo.1+2")]

        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, ExpectedResult = "0.0.2-foo.1+2")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, ExpectedResult = "0.0.3-foo.1+2")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, ExpectedResult = "0.1.0-foo.1+2")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, ExpectedResult = "1.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Inherit, ExpectedResult = "0.0.3-foo.1+2")]

        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, ExpectedResult = "0.2.0-foo.1+2")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, ExpectedResult = "0.2.1-foo.1+2")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, ExpectedResult = "0.3.0-foo.1+2")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, ExpectedResult = "1.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Inherit, ExpectedResult = "0.3.0-foo.1+2")]

        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, ExpectedResult = "2.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, ExpectedResult = "2.0.1-foo.1+2")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, ExpectedResult = "2.1.0-foo.1+2")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, ExpectedResult = "3.0.0-foo.1+2")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Inherit, ExpectedResult = "3.0.0-foo.1+2")]
        public string GetVersionWithLabelBarOnMain(IncrementStrategy incrementOnMain, IncrementStrategy increment)
        {
            var mainline = MainlineBuilder
                .WithBranch("main", b => b.WithIncrement(incrementOnMain).WithLabel("bar"))
                .WithBranch("feature", b => b.WithIncrement(increment))
                .Build();

            return this.fixture!.GetVersion(mainline).FullSemVer;
        }
    }
}
