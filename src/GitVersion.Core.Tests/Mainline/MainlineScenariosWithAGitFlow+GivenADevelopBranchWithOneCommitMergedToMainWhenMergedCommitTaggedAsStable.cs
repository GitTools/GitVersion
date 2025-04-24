using GitVersion.Configuration;
using GitVersion.Core.Tests;
using GitVersion.Core.Tests.IntegrationTests;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Mainline;

internal partial class MainlineScenariosWithAGitFlow
{
    [Parallelizable(ParallelScope.All)]
    public class GivenADevelopBranchWithOneCommitMergedToMainWhenMergedCommitTaggedAsStable
    {
        private EmptyRepositoryFixture? fixture;

        private static GitFlowConfigurationBuilder MainlineBuilder => GitFlowConfigurationBuilder.New.WithLabel(null)
            .WithVersionStrategy(VersionStrategies.Mainline)
            .WithBranch("main", b => b.WithDeploymentMode(DeploymentMode.ManualDeployment))
            .WithBranch("develop", b => b.WithDeploymentMode(DeploymentMode.ManualDeployment));

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // * 55 minutes ago  (tag: 1.0.0, main)
            // |\
            // | * 56 minutes ago  (HEAD -> develop)
            // |/
            // * 58 minutes ago

            fixture = new EmptyRepositoryFixture();

            fixture.MakeACommit("A");
            fixture.BranchTo("develop");
            fixture.MakeACommit("B");
            fixture.MergeTo("main");
            fixture.ApplyTag("1.0.0");
            fixture.Checkout("develop");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => fixture?.Dispose();

        [TestCase(IncrementStrategy.None, IncrementStrategy.None, ExpectedResult = "1.0.0-alpha.1+0")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, ExpectedResult = "1.0.1-alpha.1+0")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, ExpectedResult = "1.1.0-alpha.1+0")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, ExpectedResult = "2.0.0-alpha.1+0")]

        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, ExpectedResult = "1.0.0-alpha.1+0")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, ExpectedResult = "1.0.1-alpha.1+0")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, ExpectedResult = "1.1.0-alpha.1+0")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, ExpectedResult = "2.0.0-alpha.1+0")]

        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, ExpectedResult = "1.0.0-alpha.1+0")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, ExpectedResult = "1.0.1-alpha.1+0")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, ExpectedResult = "1.1.0-alpha.1+0")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, ExpectedResult = "2.0.0-alpha.1+0")]

        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, ExpectedResult = "1.0.0-alpha.1+0")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, ExpectedResult = "1.0.1-alpha.1+0")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, ExpectedResult = "1.1.0-alpha.1+0")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, ExpectedResult = "2.0.0-alpha.1+0")]
        public string GetVersionWithTrackMergeTargetOnDevelop(IncrementStrategy incrementOnMain, IncrementStrategy increment)
        {
            var mainline = MainlineBuilder
                .WithBranch("main", b => b.WithIncrement(incrementOnMain).WithLabel(null))
                .WithBranch("develop", b => b.WithIncrement(increment).WithTrackMergeTarget(true))
                .Build();

            return fixture!.GetVersion(mainline).FullSemVer;
        }

        [TestCase(IncrementStrategy.None, IncrementStrategy.None, ExpectedResult = "0.0.0-alpha.1+1")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Patch, ExpectedResult = "0.0.1-alpha.1+1")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Minor, ExpectedResult = "0.1.0-alpha.1+1")]
        [TestCase(IncrementStrategy.None, IncrementStrategy.Major, ExpectedResult = "1.0.0-alpha.1+1")]

        [TestCase(IncrementStrategy.Patch, IncrementStrategy.None, ExpectedResult = "0.0.1-alpha.1+1")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Patch, ExpectedResult = "0.0.2-alpha.1+1")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Minor, ExpectedResult = "0.1.0-alpha.1+1")]
        [TestCase(IncrementStrategy.Patch, IncrementStrategy.Major, ExpectedResult = "1.0.0-alpha.1+1")]

        [TestCase(IncrementStrategy.Minor, IncrementStrategy.None, ExpectedResult = "0.1.0-alpha.1+1")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Patch, ExpectedResult = "0.1.1-alpha.1+1")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Minor, ExpectedResult = "0.2.0-alpha.1+1")]
        [TestCase(IncrementStrategy.Minor, IncrementStrategy.Major, ExpectedResult = "1.0.0-alpha.1+1")]

        [TestCase(IncrementStrategy.Major, IncrementStrategy.None, ExpectedResult = "1.0.0-alpha.1+1")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Patch, ExpectedResult = "1.0.1-alpha.1+1")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Minor, ExpectedResult = "1.1.0-alpha.1+1")]
        [TestCase(IncrementStrategy.Major, IncrementStrategy.Major, ExpectedResult = "2.0.0-alpha.1+1")]
        public string GetVersionWithNoTrackMergeTargetOnDevelop(IncrementStrategy incrementOnMain, IncrementStrategy increment)
        {
            var mainline = MainlineBuilder
                .WithBranch("main", b => b.WithIncrement(incrementOnMain).WithLabel(null))
                .WithBranch("develop", b => b.WithIncrement(increment).WithTrackMergeTarget(false))
                .Build();

            return fixture!.GetVersion(mainline).FullSemVer;
        }
    }
}
