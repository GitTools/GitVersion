using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class FailingTests : TestBase
{
    [TestFixture(Description = "Failed test: Issue #1255, PR #1600")]
    public class Issue1255Pr1600
    {
        [Test(Description = "DevelopScenarios")]
        public void ShouldProvideTheCorrectVersionEvenIfPreReleaseLabelExistsInTheGitTagDevelop()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.ApplyTag("1.0.0-oreo.1");
            fixture.BranchTo("develop");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.1.0-alpha.1");
        }

        [Test(Description = "MainScenarios")]
        public void ShouldProvideTheCorrectVersionEvenIfPreReleaseLabelExistsInTheGitTagMain()
        {
            var configuration = GitFlowConfigurationBuilder.New
                .WithSemanticVersionFormat(SemanticVersionFormat.Loose)
                .WithNextVersion("5.0")
                .WithBranch(MainBranch,
                    branchBuilder => branchBuilder.WithLabel("beta")
                        .WithIncrement(IncrementStrategy.Patch)
                        .WithVersioningMode(VersioningMode.ContinuousDeployment))
                .Build();

            using EmptyRepositoryFixture fixture = new(MainBranch);
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("5.0.0-beta.0", configuration); // why not "5.0.0-beta.1"?
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("5.0.0-beta.1", configuration);
            fixture.Repository.MakeATaggedCommit("v5.0.0-rc.1");
            fixture.AssertFullSemver("5.0.0-rc.1", configuration);
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("5.0.1-beta.1", configuration); // test fails here, it generates "5.0.0-beta.1" which is not unique and lower than "5.0.0-rc.1"
        }
    }

    [TestFixture(Description = "Failed test: Issue #1844, PR #1845")]
    public class Issue1844Pr1845
    {
        [Test(Description = "VersionBumpingScenarios")]
        public void AppliedPrereleaseTagAfterBranchTagCausesVersionBump()
        {
            var configuration = GitFlowConfigurationBuilder.New
                .WithBranch(MainBranch,
                    branchBuilder => branchBuilder.WithLabel("pre")
                        .WithSourceBranches(ArraySegment<string>.Empty)
                        .WithVersioningMode(VersioningMode.ContinuousDeployment))
                .Build();

            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeACommit();
            fixture.Repository.MakeATaggedCommit("1.0.0-rc");
            fixture.Repository.MakeACommit();

            fixture.AssertFullSemver("1.0.1-pre.1", configuration);
        }
    }

    [TestFixture(Description = "Failed test: Issue #2034, PR #2059")]
    public class Issue2034Pr2059
    {
        [Test(Description = "MainlineDevelopmentMode")]
        public void MergingMainBranchToDevelopWithInheritIncrementShouldIncrementDevelopPatch()
        {
            var configuration = GitFlowConfigurationBuilder.New
                .WithAssemblyVersioningScheme(AssemblyVersioningScheme.MajorMinorPatch)
                .WithVersioningMode(VersioningMode.Mainline)
                .WithBranch(MainBranch, branchBuilder => branchBuilder.WithIncrement(IncrementStrategy.Patch))
                .WithBranch("develop", branchBuilder => branchBuilder.WithIncrement(IncrementStrategy.Inherit))
                .Build();

            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeACommit($"initial in {MainBranch}");
            fixture.AssertFullSemver("0.1.0", configuration);
            fixture.MakeACommit($"{MainBranch} change");
            fixture.AssertFullSemver("0.1.1", configuration);

            fixture.BranchTo("develop");
            fixture.AssertFullSemver("0.1.2-alpha.0", configuration);
            fixture.MakeACommit("develop change");
            fixture.AssertFullSemver("0.1.2-alpha.1", configuration);

            fixture.Checkout(MainBranch);
            fixture.MakeACommit($"{MainBranch} hotfix");
            fixture.AssertFullSemver("0.1.2", configuration);

            fixture.Checkout("develop");
            fixture.MergeNoFF(MainBranch);
            fixture.AssertFullSemver("0.1.3-alpha.1", configuration);
        }
    }

    [TestFixture(Description = "Failed test: Issue #2693, PR #2696")]
    public class Issue2693Pr2696
    {
        [Test(Description = "HotfixBranchScenarios")]
        public void VersionNumberInHotfixBranchShouldBeConsideredWhenPreventIncrementOfMergedBranchVersion()
        {
            var configuration = GitFlowConfigurationBuilder.New
                .WithAssemblyVersioningScheme(AssemblyVersioningScheme.MajorMinorPatchTag)
                .WithAssemblyFileVersioningFormat("{MajorMinorPatch}.0")
                .WithVersioningMode(VersioningMode.ContinuousDeployment)
                .WithBranch("hotfix",
                    branchBuilder => branchBuilder
                        .WithPreventIncrementOfMergedBranchVersion(true)
                        .WithRegularExpression("r^(origin/)?hotfix[/-]")
                )
                .Build();

            const string HotfixBranch = "hotfix/1.1.1";
            const string ReleaseBranch = "release/1.1.0";

            using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch(ReleaseBranch));
            fixture.MakeACommit();
            Commands.Checkout(fixture.Repository, MainBranch);
            fixture.MergeNoFF(ReleaseBranch);
            fixture.Repository.CreateBranch(HotfixBranch);
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.1.1-ci.1", configuration);
        }
    }

    [TestFixture(Description = "Failed test: Issue #2821, PR #2830")]
    public class Issue2821Pr2830 : TestBase
    {
        private readonly GitVersionConfiguration configuration = GitFlowConfigurationBuilder.New
            .WithVersioningMode(VersioningMode.Mainline)
            .WithBranch("feature", branchBuilder => branchBuilder.WithIncrement(IncrementStrategy.Minor))
            .WithBranch("pull-request", branchBuilder => branchBuilder.WithIncrement(IncrementStrategy.Minor))
            .WithBranch("support",
                branchBuilder => branchBuilder
                    .WithVersioningMode(VersioningMode.ContinuousDeployment)
                    .WithLabel("beta")
                    .WithIncrement(IncrementStrategy.Patch))
            .Build();

        [Test]
        public void IncrementFeatureByMinor()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.MakeATaggedCommit("0.1.0");

            // feature workflow
            fixture.BranchTo("feature/foo", "foo");
            fixture.MakeACommit();
            fixture.AssertFullSemver("0.2.0-foo.1", this.configuration);
            fixture.MakeACommit();
            fixture.AssertFullSemver("0.2.0-foo.2", this.configuration);
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("feature/foo");
            fixture.AssertFullSemver("0.2.0", this.configuration);
        }

        [Test]
        public void CanCalculatePullRequestChanges()
        {
            using var fixture = new EmptyRepositoryFixture();
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.MakeATaggedCommit("1.1.0");
            fixture.Repository.MakeATaggedCommit("2.0.0");

            // feature branch
            Commands.Checkout(fixture.Repository, fixture.Repository.CreateBranch("feature/foo"));
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("2.1.0-foo.1", this.configuration);
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("2.1.0-foo.2", this.configuration);

            // pull request
            fixture.Repository.CreatePullRequestRef("feature/foo", MainBranch, normalise: true);
            fixture.AssertFullSemver("2.1.0-PullRequest0002.3", this.configuration);
            Commands.Checkout(fixture.Repository, MainBranch);
            fixture.Repository.MergeNoFF("feature/foo", Generate.SignatureNow());
            fixture.AssertFullSemver("2.1.0", this.configuration);
            fixture.Repository.MakeATaggedCommit("2.1.0"); // must tag before pull of any hotfix otherwise hotfix stays at this version

            // hotfix branch
            var tag = fixture.Repository.Tags.Single(t => t.FriendlyName == "1.0.0");
            var supportBranch = fixture.Repository.CreateBranch("support/1.0.0", (LibGit2Sharp.Commit)tag.Target);
            Commands.Checkout(fixture.Repository, supportBranch);
            fixture.AssertFullSemver("1.0.0", this.configuration);
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.0.1-beta.1", this.configuration);
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.0.1-beta.2", this.configuration);
            fixture.Repository.MakeATaggedCommit("1.0.1");
            fixture.Repository.MakeACommit();
            fixture.AssertFullSemver("1.0.2-beta.1", this.configuration);

            // pull request
            fixture.Repository.CreatePullRequestRef("support/1.0.0", MainBranch, 3, normalise: true);
            fixture.Repository.DumpGraph();
            fixture.AssertFullSemver("2.1.1-PullRequest0003.6", this.configuration);
            Commands.Checkout(fixture.Repository, MainBranch);
            fixture.Repository.MergeNoFF("support/1.0.0", Generate.SignatureNow());
            fixture.AssertFullSemver("2.1.1", this.configuration);
        }
    }

    [TestFixture(Description = "Failed test: Issue #2786, PR #2787")]
    public class Issue2786Pr2787
    {
        [Test(Description = "MainlineDevelopmentMode")]
        public void HotfixBranchesWithTaggedCommitsOnMain()
        {
            using var fixture = new EmptyRepositoryFixture();
            var configuration = GitFlowConfigurationBuilder.New
                .WithVersioningMode(VersioningMode.Mainline)
                .WithIncrement(IncrementStrategy.Minor)
                .WithBranch(ConfigurationConstants.MainBranchKey,
                    branchBuilder => branchBuilder
                        .WithRegularExpression(ConfigurationConstants.MainBranchRegex)
                        .WithSourceBranches(ConfigurationConstants.DevelopBranchKey, ConfigurationConstants.ReleaseBranchKey)
                        .WithLabel("")
                        .WithPreventIncrementOfMergedBranchVersion(true)
                        .WithIncrement(IncrementStrategy.Minor)
                        .WithIsMainline(true)
                        .WithPreReleaseWeight(55000)
                )
                .WithBranch(ConfigurationConstants.HotfixBranchKey, branchBuilder => branchBuilder.WithLabel(""))
                .Build();

            fixture.Repository.MakeACommit("1");
            fixture.MakeATaggedCommit("1.0.0");

            fixture.MakeACommit(); // 1.1.0
            fixture.AssertFullSemver("1.1.0", configuration);
            fixture.ApplyTag("1.1.0");
            fixture.AssertFullSemver("1.1.0", configuration);

            fixture.BranchTo("hotfix/may");
            fixture.AssertFullSemver("1.1.1", configuration);

            // Move main on
            fixture.Checkout(MainBranch);
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.2.0", configuration);

            // Continue on hotfix
            fixture.Checkout("hotfix/may");
            fixture.MakeACommit(); // 1.2.1
            fixture.AssertFullSemver("1.1.1", configuration);
        }

        [Test(Description = "MainlineDevelopmentMode")]
        public void HotfixBranchesWithTaggedCommitsOnHotfix()
        {
            using var fixture = new EmptyRepositoryFixture();
            var configuration = GitFlowConfigurationBuilder.New
                .WithVersioningMode(VersioningMode.Mainline)
                .WithIncrement(IncrementStrategy.Minor)
                .WithBranch(ConfigurationConstants.MainBranchKey,
                    branchBuilder => branchBuilder
                        .WithRegularExpression(ConfigurationConstants.MainBranchRegex)
                        .WithSourceBranches(ConfigurationConstants.DevelopBranchKey, ConfigurationConstants.ReleaseBranchKey)
                        .WithLabel("")
                        .WithPreventIncrementOfMergedBranchVersion(true)
                        .WithIncrement(IncrementStrategy.Minor)
                        .WithIsMainline(true)
                        .WithPreReleaseWeight(55000)
                )
                .WithBranch(ConfigurationConstants.HotfixBranchKey, branchBuilder => branchBuilder.WithLabel(""))
                .Build();

            fixture.Repository.MakeACommit("1");
            fixture.MakeATaggedCommit("1.0.0");

            fixture.MakeACommit(); // 1.1.0
            fixture.AssertFullSemver("1.1.0", configuration);
            fixture.ApplyTag("1.1.0");
            fixture.AssertFullSemver("1.1.0", configuration);
            fixture.MakeACommit(); // 1.2.0
            fixture.AssertFullSemver("1.2.0", configuration);

            fixture.BranchTo("hotfix/may");
            fixture.AssertFullSemver("1.2.1", configuration);

            // Move main on
            fixture.Checkout(MainBranch);
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.3.0", configuration);

            // Continue on hotfix
            fixture.Checkout("hotfix/may");
            fixture.MakeACommit(); // 1.2.1
            fixture.MakeATaggedCommit("1.2.2");
            fixture.MakeACommit(); // 1.2.3
            fixture.AssertFullSemver("1.2.3", configuration);
        }
    }

    [TestFixture(Description = "Failed test: Issue #2454, PR #2847")]
    public class Issue2454Pr2847
    {
        [Test(Description = "HotfixBranchScenarios")]
        public void HotfixMergeIncrementsVersionWithModeContinuousDeployment()
        {
            var configuration = GitFlowConfigurationBuilder.New
                .WithAssemblyVersioningScheme(AssemblyVersioningScheme.MajorMinorPatchTag)
                .WithVersioningMode(VersioningMode.ContinuousDeployment)
                .Build();

            using var fixture = new EmptyRepositoryFixture();

            // initialize gitflow

            const string devBranch = "develop";

            fixture.Repository.MakeACommit("setup repo");
            fixture.Repository.CreateBranch(devBranch);
            Commands.Checkout(fixture.Repository, devBranch);

            // make some changes on dev

            fixture.Repository.MakeACommit("add stuff");
            fixture.Repository.MakeACommit("add more stuff");

            // start a release

            const string releaseBranch = "release/1.0";

            fixture.Repository.CreateBranch(releaseBranch);
            Commands.Checkout(fixture.Repository, releaseBranch);
            fixture.Repository.MakeACommit("fix some minor thing");

            fixture.AssertFullSemver("1.0.0-beta.1", configuration);

            Commands.Checkout(fixture.Repository, MainBranch);
            fixture.Repository.MergeNoFF(releaseBranch, Generate.SignatureNow());

            fixture.AssertFullSemver("1.0.0-ci.0", configuration);
            fixture.ApplyTag("1.0");
            fixture.AssertFullSemver("1.0.0", configuration);

            // start first hotfix

            const string hotfixBranch = "hotfix/something-important";

            fixture.Repository.CreateBranch(hotfixBranch);
            fixture.Repository.MakeACommit("fix the important issue");
            // fixture.AssertFullSemver("1.0.1-beta.1"); // FAILS, not sure if hotfixes should have beta tag
            fixture.AssertFullSemver("1.0.1-ci.1", configuration); // PASSES
            fixture.Repository.MakeACommit("fix something else");
            // fixture.AssertFullSemver("1.0.1-beta.2"); // FAILS, not sure if hotfixes should have beta tag
            fixture.AssertFullSemver("1.0.1-ci.2", configuration); // PASSES

            Commands.Checkout(fixture.Repository, MainBranch);
            fixture.Repository.MergeNoFF(hotfixBranch, Generate.SignatureNow());

            fixture.AssertFullSemver("1.0.1-ci.2", configuration);

            // start second hotfix

            const string hotfix2Branch = "hotfix/another-important-thing";

            fixture.Repository.CreateBranch(hotfix2Branch);
            fixture.Repository.MakeACommit("fix the new issue");
            // fixture.AssertFullSemver("1.0.2-beta.1"); // FAILS, not sure if hotfixes should have beta tag
            fixture.AssertFullSemver("1.0.2-ci.1", configuration); // FAILS, version is 1.0.1-ci.3

            Commands.Checkout(fixture.Repository, MainBranch);
            fixture.Repository.MergeNoFF(hotfix2Branch, Generate.SignatureNow());

            fixture.AssertFullSemver("1.0.2-ci.1", configuration); // FAILS, version is 1.0.1-ci.3
        }

        [Test]
        public void HotfixMergeIncrementsVersionWithDefaultConfig()
        {
            var configuration = GitFlowConfigurationBuilder.New.Build();

            using var fixture = new EmptyRepositoryFixture();

            // initialize gitflow

            const string devBranch = "develop";

            fixture.Repository.MakeACommit("setup repo");
            fixture.Repository.CreateBranch(devBranch);
            Commands.Checkout(fixture.Repository, devBranch);

            // make some changes on dev

            fixture.Repository.MakeACommit("add stuff");
            fixture.Repository.MakeACommit("add more stuff");

            // start a release

            const string releaseBranch = "release/1.0";

            fixture.Repository.CreateBranch(releaseBranch);
            Commands.Checkout(fixture.Repository, releaseBranch);
            fixture.Repository.MakeACommit("fix some minor thing");

            fixture.AssertFullSemver("1.0.0-beta.1+1", configuration);

            Commands.Checkout(fixture.Repository, MainBranch);
            fixture.Repository.MergeNoFF(releaseBranch, Generate.SignatureNow());

            fixture.AssertFullSemver("1.0.0+0", configuration);
            fixture.ApplyTag("1.0");
            fixture.AssertFullSemver("1.0.0", configuration);

            // start first hotfix

            const string hotfixBranch = "hotfix/something-important";

            fixture.Repository.CreateBranch(hotfixBranch);
            fixture.Repository.MakeACommit("fix the important issue");
            // fixture.AssertFullSemver("1.0.1-beta.1+1", config); // FAILS, not sure if hotfixes should have beta tag
            fixture.AssertFullSemver("1.0.1+1", configuration);
            fixture.Repository.MakeACommit("fix something else");
            // fixture.AssertFullSemver("1.0.1-beta.2+2", config); // FAILS, not sure if hotfixes should have beta tag
            fixture.AssertFullSemver("1.0.1+2", configuration);

            Commands.Checkout(fixture.Repository, MainBranch);
            fixture.Repository.MergeNoFF(hotfixBranch, Generate.SignatureNow());

            fixture.AssertFullSemver("1.0.1+2", configuration);

            // start second hotfix

            const string hotfix2Branch = "hotfix/another-important-thing";

            fixture.Repository.CreateBranch(hotfix2Branch);
            fixture.Repository.MakeACommit("fix the new issue");
            // fixture.AssertFullSemver("1.0.2-beta.1+1", config); // FAILS, not sure if hotfixes should have beta tag
            fixture.AssertFullSemver("1.0.2+1", configuration); // FAILS, version is 1.0.1+3

            Commands.Checkout(fixture.Repository, MainBranch);
            fixture.Repository.MergeNoFF(hotfix2Branch, Generate.SignatureNow());

            fixture.AssertFullSemver("1.0.2+1", configuration); // FAILS, version is 1.0.1+3
        }
    }
}
