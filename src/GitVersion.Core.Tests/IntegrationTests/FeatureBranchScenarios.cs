using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class FeatureBranchScenarios : TestBase
{
    [Test]
    public void ShouldInheritIncrementCorrectlyWithMultiplePossibleParentsAndWeirdlyNamedDevelopBranch()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.CreateBranch("development");
        Commands.Checkout(fixture.Repository, "development");

        //Create an initial feature branch
        var feature123 = fixture.Repository.CreateBranch("feature/JIRA-123");
        Commands.Checkout(fixture.Repository, "feature/JIRA-123");
        fixture.Repository.MakeCommits(1);

        //Merge it
        Commands.Checkout(fixture.Repository, "development");
        fixture.Repository.Merge(feature123, Generate.SignatureNow());

        //Create a second feature branch
        fixture.Repository.CreateBranch("feature/JIRA-124");
        Commands.Checkout(fixture.Repository, "feature/JIRA-124");
        fixture.Repository.MakeCommits(1);

        fixture.AssertFullSemver("1.1.0-JIRA-124.1+2");
    }

    [Test]
    public void BranchCreatedAfterFastForwardMergeShouldInheritCorrectly()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("unstable", builder => builder
                .WithIncrement(IncrementStrategy.Minor)
                .WithRegularExpression("unstable")
                .WithSourceBranches()
                .WithIsSourceBranchFor("feature"))
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.CreateBranch("unstable");
        Commands.Checkout(fixture.Repository, "unstable");

        //Create an initial feature branch
        var feature123 = fixture.Repository.CreateBranch("feature/JIRA-123");
        Commands.Checkout(fixture.Repository, "feature/JIRA-123");
        fixture.Repository.MakeCommits(1);

        //Merge it
        Commands.Checkout(fixture.Repository, "unstable");
        fixture.Repository.Merge(feature123, Generate.SignatureNow());

        //Create a second feature branch
        fixture.Repository.CreateBranch("feature/JIRA-124");
        Commands.Checkout(fixture.Repository, "feature/JIRA-124");
        fixture.Repository.MakeCommits(1);

        fixture.AssertFullSemver("1.1.0-JIRA-124.1+2", configuration);
    }

    [Test]
    public void ShouldNotUseNumberInFeatureBranchAsPreReleaseNumberOffDevelop()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.CreateBranch("develop");
        Commands.Checkout(fixture.Repository, "develop");
        fixture.Repository.CreateBranch("feature/JIRA-123");
        Commands.Checkout(fixture.Repository, "feature/JIRA-123");
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("1.1.0-JIRA-123.1+5");
    }

    [Test]
    public void ShouldNotUseNumberInFeatureBranchAsPreReleaseNumberOffMain()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.CreateBranch("feature/JIRA-123");
        Commands.Checkout(fixture.Repository, "feature/JIRA-123");
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("1.0.1-JIRA-123.1+5");
    }

    [Test]
    public void TestFeatureBranch()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.CreateBranch("feature-test");
        Commands.Checkout(fixture.Repository, "feature-test");
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("1.0.1-test.1+5");
    }

    [Test]
    public void TestFeaturesBranch()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("1.0.0");
        fixture.Repository.CreateBranch("features/test");
        Commands.Checkout(fixture.Repository, "features/test");
        fixture.Repository.MakeCommits(5);

        fixture.AssertFullSemver("1.0.1-test.1+5");
    }

    [Test]
    public void WhenTwoFeatureBranchPointToTheSameCommit()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();
        fixture.Repository.CreateBranch("develop");
        Commands.Checkout(fixture.Repository, "develop");
        fixture.Repository.CreateBranch("feature/feature1");
        Commands.Checkout(fixture.Repository, "feature/feature1");
        fixture.Repository.MakeACommit();
        fixture.Repository.CreateBranch("feature/feature2");
        Commands.Checkout(fixture.Repository, "feature/feature2");

        fixture.AssertFullSemver("0.1.0-feature2.1+2");
    }

    [Test]
    public void ShouldBePossibleToMergeDevelopForALongRunningBranchWhereDevelopAndMainAreEqual()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeATaggedCommit("v1.0.0");

        fixture.Repository.CreateBranch("develop");
        Commands.Checkout(fixture.Repository, "develop");

        const string branchName = "feature/longrunning";
        fixture.Repository.CreateBranch(branchName);
        Commands.Checkout(fixture.Repository, branchName);
        fixture.Repository.MakeACommit();

        Commands.Checkout(fixture.Repository, "develop");
        fixture.Repository.MakeACommit();

        Commands.Checkout(fixture.Repository, MainBranch);
        fixture.Repository.Merge(fixture.Repository.Branches["develop"], Generate.SignatureNow());
        fixture.Repository.ApplyTag("v1.1.0");

        Commands.Checkout(fixture.Repository, branchName);
        fixture.Repository.Merge(fixture.Repository.Branches["develop"], Generate.SignatureNow());

        var configuration = GitFlowConfigurationBuilder.New
            .WithDeploymentMode(DeploymentMode.ContinuousDelivery)
            .WithBranch("feature", builder => builder.WithDeploymentMode(DeploymentMode.ContinuousDelivery))
            .Build();
        fixture.AssertFullSemver("1.2.0-longrunning.2", configuration);
    }

    [Test]
    public void CanUseBranchNameOffAReleaseBranch()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("release", builder => builder.WithLabel("build"))
            .WithBranch("feature", builder => builder.WithLabel(ConfigurationConstants.BranchNamePlaceholder))
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("release/0.3.0");
        fixture.MakeATaggedCommit("v0.3.0-build.1");
        fixture.MakeACommit();
        fixture.BranchTo("feature/PROJ-1");
        fixture.MakeACommit();

        fixture.AssertFullSemver("0.3.0-PROJ-1.1+4", configuration);
    }

    [TestCase("alpha", "JIRA-123", "^features?[/-](?<BranchName>.+)", "alpha")]
    [TestCase($"alpha.{ConfigurationConstants.BranchNamePlaceholder}", "JIRA-123", "^features?[/-](?<BranchName>.+)", "alpha.JIRA-123")]
    [TestCase("{BranchName}-of-task-number-{TaskNumber}", "4711_this-is-a-feature", "^features?[/-](?<TaskNumber>\\d+)_(?<BranchName>.+)", "this-is-a-feature-of-task-number-4711")]
    public void ShouldUseConfiguredLabel(string label, string featureName, string regularExpression, string preReleaseLabelName)
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithBranch("feature", builder => builder
                .WithLabel(label)
                .WithRegularExpression(regularExpression))
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeATaggedCommit("1.0.0");
        var featureBranchName = $"feature/{featureName}";
        fixture.BranchTo(featureBranchName);
        fixture.Repository.MakeCommits(5);

        var expectedFullSemVer = $"1.0.1-{preReleaseLabelName}.1+5";
        fixture.AssertFullSemver(expectedFullSemVer, configuration);
    }

    [Test]
    public void BranchCreatedAfterFinishReleaseShouldInheritAndIncrementFromLastMainCommitTag()
    {
        using var fixture = new BaseGitFlowRepositoryFixture("0.1.0");
        //validate current version
        fixture.AssertFullSemver("0.2.0-alpha.1");
        fixture.Repository.CreateBranch("release/0.2.0");
        Commands.Checkout(fixture.Repository, "release/0.2.0");

        //validate release version
        fixture.AssertFullSemver("0.2.0-beta.1+1");

        fixture.Checkout(MainBranch);
        fixture.Repository.MergeNoFF("release/0.2.0");
        fixture.Repository.ApplyTag("0.2.0");

        //validate main branch version
        fixture.AssertFullSemver("0.2.0");

        fixture.Checkout("develop");
        fixture.Repository.MergeNoFF("release/0.2.0");
        fixture.Repository.Branches.Remove("release/2.0.0");

        fixture.Repository.MakeACommit();

        //validate develop branch version after merging release 0.2.0 to main and develop (finish release)
        fixture.AssertFullSemver("0.3.0-alpha.1");

        //create a feature branch from develop
        fixture.BranchTo("feature/TEST-1");
        fixture.Repository.MakeACommit();

        //I'm not entirely sure what the + value should be but I know the semvar major/minor/patch should be 0.3.0
        fixture.AssertFullSemver("0.3.0-TEST-1.1+2");
    }

    [Test]
    public void ShouldPickUpVersionFromDevelopAfterReleaseBranchCreated()
    {
        using var fixture = new EmptyRepositoryFixture();
        // Create develop and release branches
        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("release/1.0.0");
        fixture.MakeACommit();
        fixture.Checkout("develop");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-alpha.1");

        // create a feature branch from develop and verify the version
        fixture.BranchTo("feature/test");
        fixture.AssertFullSemver("1.1.0-test.1+1");
    }

    [Test]
    public void ShouldPickUpVersionFromDevelopAfterReleaseBranchMergedBack()
    {
        using var fixture = new EmptyRepositoryFixture();
        // Create develop and release branches
        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("release/1.0.0");
        fixture.MakeACommit();

        // merge release into develop
        fixture.Checkout("develop");
        fixture.MergeNoFF("release/1.0.0");
        fixture.AssertFullSemver("1.1.0-alpha.2");

        // create a feature branch from develop and verify the version
        fixture.BranchTo("feature/test");
        fixture.AssertFullSemver("1.1.0-test.1+2");
    }

    public class WhenMainAsIsDevelop
    {
        [Test]
        public void ShouldPickUpVersionFromMainAfterReleaseBranchCreated()
        {
            var configuration = GitFlowConfigurationBuilder.New
                .WithBranch(MainBranch, builder => builder.WithTracksReleaseBranches(true))
                .Build();

            using var fixture = new EmptyRepositoryFixture();
            // Create release branch
            fixture.MakeACommit();
            fixture.BranchTo("release/1.0.0");
            fixture.MakeACommit();
            fixture.Checkout(MainBranch);
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.0.1-1", configuration);

            // create a feature branch from main and verify the version
            fixture.BranchTo("feature/test");
            fixture.AssertFullSemver("1.0.1-test.1+1", configuration);
        }

        [Test]
        public void ShouldPickUpVersionFromMainAfterReleaseBranchMergedBack()
        {
            var configuration = GitFlowConfigurationBuilder.New
                .WithBranch(MainBranch, builder => builder.WithTracksReleaseBranches(true))
                .Build();

            using var fixture = new EmptyRepositoryFixture();
            // Create release branch
            fixture.MakeACommit();
            fixture.BranchTo("release/1.0.0");
            fixture.MakeACommit();

            // merge release into main
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("release/1.0.0");
            fixture.AssertFullSemver("1.0.1-2", configuration);

            // create a feature branch from main and verify the version
            fixture.BranchTo("feature/test");
            fixture.AssertFullSemver("1.0.1-test.1+2", configuration);
        }
    }

    public class WhenFeatureBranchHasNoConfig
    {
        [Test]
        public void ShouldPickUpVersionFromMainAfterReleaseBranchCreated()
        {
            using var fixture = new EmptyRepositoryFixture();
            // Create develop and release branches
            fixture.MakeACommit();
            fixture.BranchTo("develop");
            fixture.MakeACommit();
            fixture.BranchTo("release/1.0.0");
            fixture.MakeACommit();
            fixture.Checkout("develop");
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.1.0-alpha.1");

            // create a misnamed feature branch (i.e. it uses the default configuration) from develop and verify the version
            fixture.BranchTo("misnamed");
            fixture.AssertFullSemver("1.1.0-misnamed.1+1");
        }

        [Test]
        public void ShouldPickUpVersionFromDevelopAfterReleaseBranchMergedBack()
        {
            using var fixture = new EmptyRepositoryFixture();
            // Create develop and release branches
            fixture.MakeACommit();
            fixture.BranchTo("develop");
            fixture.MakeACommit();
            fixture.BranchTo("release/1.0.0");
            fixture.MakeACommit();

            // merge release into develop
            fixture.Checkout("develop");
            fixture.MergeNoFF("release/1.0.0");
            fixture.AssertFullSemver("1.1.0-alpha.2");

            // create a misnamed feature branch (i.e. it uses the default configuration) from develop and verify the version
            fixture.BranchTo("misnamed");
            fixture.AssertFullSemver("1.1.0-misnamed.1+2");
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public class WhenMainMarkedAsIsDevelop
        {
            [Test]
            public void ShouldPickUpVersionFromMainAfterReleaseBranchCreated()
            {
                var configuration = GitFlowConfigurationBuilder.New
                    .WithBranch(MainBranch, builder => builder.WithTracksReleaseBranches(true))
                    .Build();

                using var fixture = new EmptyRepositoryFixture();
                // Create release branch
                fixture.MakeACommit();
                fixture.BranchTo("release/1.0.0");
                fixture.MakeACommit();
                fixture.Checkout(MainBranch);
                fixture.MakeACommit();
                fixture.AssertFullSemver("1.0.1-1", configuration);

                // create a misnamed feature branch (i.e. it uses the default configuration) from main and verify the version
                fixture.BranchTo("misnamed");
                fixture.AssertFullSemver("1.0.1-misnamed.1+1", configuration);
            }

            [Test]
            public void ShouldPickUpVersionFromMainAfterReleaseBranchMergedBack()
            {
                var configuration = GitFlowConfigurationBuilder.New
                    .WithBranch(MainBranch, builder => builder.WithTracksReleaseBranches(true))
                    .Build();

                using var fixture = new EmptyRepositoryFixture();
                // Create release branch
                fixture.MakeACommit();
                fixture.BranchTo("release/1.0.0");
                fixture.MakeACommit();

                // merge release into main
                fixture.Checkout(MainBranch);
                fixture.MergeNoFF("release/1.0.0");
                fixture.AssertFullSemver("1.0.1-2", configuration);

                // create a misnamed feature branch (i.e. it uses the default configuration) from main and verify the version
                fixture.BranchTo("misnamed");
                fixture.AssertFullSemver("1.0.1-misnamed.1+2", configuration);
            }
        }
    }

    [Test]
    public void PickUpVersionFromMainMarkedWithIsTracksReleaseBranches()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithDeploymentMode(DeploymentMode.ManualDeployment)
            .WithBranch("unknown", builder => builder.WithIncrement(IncrementStrategy.Patch).WithTracksReleaseBranches(true))
            .WithBranch(MainBranch, builder => builder.WithLabel("pre").WithTracksReleaseBranches(true))
            .WithBranch("release", builder => builder.WithLabel("rc"))
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();

        // create a release branch and tag a release
        fixture.BranchTo("release/0.10.0");
        fixture.MakeACommit();
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.10.0-rc.1+3", configuration);

        // switch to main and verify the version
        fixture.Checkout(MainBranch);
        fixture.MakeACommit();
        fixture.AssertFullSemver("0.10.1-pre.1+1", configuration);

        // create a feature branch from main and verify the version
        fixture.BranchTo("MyFeatureD");
        fixture.AssertFullSemver("0.10.1-MyFeatureD.1+1", configuration);
    }

    [Test]
    public void ShouldHaveAGreaterSemVerAfterDevelopIsMergedIntoFeature()
    {
        var configuration = GitFlowConfigurationBuilder.New
            .WithAssemblyVersioningScheme(AssemblyVersioningScheme.Major)
            .WithAssemblyFileVersioningFormat("{MajorMinorPatch}.{env:WeightedPreReleaseNumber ?? 0}")
            .WithBranch("main", builder => builder.WithDeploymentMode(DeploymentMode.ContinuousDelivery))
            .WithBranch("develop", builder => builder.WithPreventIncrementOfMergedBranch(true))
            .WithBranch("feature", builder => builder
                .WithLabel($"feat-{ConfigurationConstants.BranchNamePlaceholder}")
                .WithDeploymentMode(DeploymentMode.ContinuousDelivery)
            )
            .WithCommitMessageIncrementing(CommitMessageIncrementMode.Disabled)
            .Build();

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.ApplyTag("16.23.0");
        fixture.MakeACommit();
        fixture.BranchTo("feature/featX");
        fixture.MakeACommit();
        fixture.Checkout("develop");
        fixture.MakeACommit();
        fixture.Checkout("feature/featX");
        fixture.MergeNoFF("develop");
        fixture.AssertFullSemver("16.24.0-feat-featX.4", configuration);
    }
}
