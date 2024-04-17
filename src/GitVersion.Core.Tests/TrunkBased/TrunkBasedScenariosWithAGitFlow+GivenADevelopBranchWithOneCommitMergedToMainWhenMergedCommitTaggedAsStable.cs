using GitVersion.Configuration;
using GitVersion.Core.Tests;
using GitVersion.Core.Tests.IntegrationTests;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.TrunkBased;

internal partial class TrunkBasedScenariosWithAGitFlow
{
    [Parallelizable(ParallelScope.All)]
    public class GivenADevelopBranchWithOneCommitMergedToMainWhenMergedCommitTaggedAsStable
    {
        private EmptyRepositoryFixture? fixture;

        private static GitFlowConfigurationBuilder TrunkBasedBuilder => GitFlowConfigurationBuilder.New.WithLabel(null)
            .WithVersionStrategy(VersionStrategies.TrunkBased)
            .WithBranch("main", _ => _.WithDeploymentMode(DeploymentMode.ManualDeployment))
            .WithBranch("develop", _ => _.WithDeploymentMode(DeploymentMode.ManualDeployment));

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
            IGitVersionConfiguration trunkBased = TrunkBasedBuilder
                .WithBranch("main", _ => _.WithIncrement(incrementOnMain).WithLabel(null))
                .WithBranch("develop", _ => _.WithIncrement(increment).WithTrackMergeTarget(true))
                .Build();

            return fixture!.GetVersion(trunkBased).FullSemVer;
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
            IGitVersionConfiguration trunkBased = TrunkBasedBuilder
                .WithBranch("main", _ => _.WithIncrement(incrementOnMain).WithLabel(null))
                .WithBranch("develop", _ => _.WithIncrement(increment).WithTrackMergeTarget(false))
                .Build();

            return fixture!.GetVersion(trunkBased).FullSemVer;
        }
    }
}
