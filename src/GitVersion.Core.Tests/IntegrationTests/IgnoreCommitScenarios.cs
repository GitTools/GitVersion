using GitVersion.Configuration;
using GitVersion.Testing.Extensions;

namespace GitVersion.Tests.IntegrationTests;

[TestFixture]
public class IgnoreCommitScenarios : TestBase
{
    [Test]
    public void ShouldThrowGitVersionExceptionWhenAllCommitsAreIgnored()
    {
        using var fixture = new EmptyRepositoryFixture();
        var dateTimeNow = DateTimeOffset.Now;
        fixture.MakeACommit();

        var configuration = GitFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Before = dateTimeNow.AddYears(1) }).Build();

        Should.Throw<GitVersionException>(() => fixture.GetVersion(configuration))
            .Message.ShouldBe("No commits found on the current branch.");
    }

    [TestCase(null, "0.0.1-1")]
    [TestCase("0.0.1", "0.0.1-1")]
    [TestCase("0.1.0", "0.1.0-1")]
    [TestCase("1.0.0", "1.0.0-1")]
    public void ShouldNotFallbackToBaseVersionWhenAllCommitsAreNotIgnored(string? nextVersion, string expectedFullSemVer)
    {
        using var fixture = new EmptyRepositoryFixture();
        var dateTimeNow = DateTimeOffset.Now;
        fixture.MakeACommit();

        var configuration = GitFlowConfigurationBuilder.New.WithNextVersion(nextVersion)
            .WithIgnoreConfiguration(new IgnoreConfiguration { Before = dateTimeNow.AddYears(-1) }).Build();

        fixture.AssertFullSemver(expectedFullSemVer, configuration);
    }

    [Test]
    public void GivenTrunkBasedWorkflowWithCommitParameterThenVersionShouldBeCorrect()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");
        var commitB = fixture.Repository.MakeACommit("B");
        fixture.MakeACommit("C");
        fixture.MakeACommit("D");

        var configuration = TrunkBasedConfigurationBuilder.New.Build();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2", configuration, commitId: commitB.Sha);
    }

    [Test]
    public void GivenTrunkBasedWorkflowWithIgnoreConfigurationForCommitThenVersionShouldBeCorrect()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");
        var commitB = fixture.Repository.MakeACommit("B");
        fixture.MakeACommit("C");
        fixture.MakeACommit("D");

        var configuration = TrunkBasedConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Shas = [commitB.Sha] })
            .Build();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3", configuration);
    }

    [Test]
    public void GivenTrunkBasedWorkflowWithIgnoreConfigurationForCommitBAndCommitParameterAThenVersionShouldBeCorrect()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");
        var commitB = fixture.Repository.MakeACommit("B");
        var commitC = fixture.Repository.MakeACommit("C");
        fixture.MakeACommit("D");

        var configuration = TrunkBasedConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Shas = [commitB.Sha] })
            .Build();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2", configuration, commitId: commitC.Sha);
    }

    [Test]
    public void GivenTrunkBasedWorkflowWithIgnoreConfigurationForCommitCAndCommitParameterCThenCommitBShouldBeUsed()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");
        fixture.MakeACommit("B");
        var commitC = fixture.Repository.MakeACommit("C");

        var configuration = TrunkBasedConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Shas = [commitC.Sha] })
            .Build();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.2", configuration, commitId: commitC.Sha);
    }

    [Test]
    public void GivenTrunkBasedWorkflowWithIgnoreConfigurationForTaggedCommitThenTagShouldBeIgnored()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");
        fixture.MakeACommit("B");
        var commitC = fixture.Repository.MakeACommit("C");
        fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("D");

        var configuration = TrunkBasedConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Shas = [commitC.Sha] })
            .Build();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.3", configuration);
    }

    [Test]
    public void GivenTrunkBasedWorkflowWithIgnoreConfigurationBeforeCommitWithTagThenTagShouldBeIgnored()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");
        fixture.MakeACommit("B");
        var commitC = fixture.Repository.MakeACommit("C");
        fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("D");

        var before = commitC.Committer.When.AddSeconds(1);
        var configuration = TrunkBasedConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Before = before })
            .Build();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1", configuration);
    }

    [TestCase(false, "1.0.1-0")]
    [TestCase(true, "1.0.0")]
    public void GivenTrunkBasedWorkflowWithIgnoreConfigurationOfCommitBThenTagShouldBeConsidered(
        bool preventIncrementWhenCurrentCommitTagged, string semanticVersion)
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");
        fixture.ApplyTag("1.0.0");
        var commitB = fixture.Repository.MakeACommit("B");

        var configuration = TrunkBasedConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Shas = [commitB.Sha] })
            .WithBranch("main", b => b.WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementWhenCurrentCommitTagged(preventIncrementWhenCurrentCommitTagged)
                .WithDeploymentMode(GitVersion.VersionCalculation.DeploymentMode.ContinuousDelivery)
            ).Build();

        // ✅ succeeds as expected
        fixture.AssertFullSemver(semanticVersion, configuration);
    }

    [TestCase(false, "1.0.1-0")]
    [TestCase(true, "1.0.0")]
    public void GivenTrunkBasedWorkflowWithCommitParameterBThenTagShouldBeConsidered(
        bool preventIncrementWhenCurrentCommitTagged, string semanticVersion)
    {
        using var fixture = new EmptyRepositoryFixture();

        var commitA = fixture.Repository.MakeACommit("A");
        fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        var configuration = TrunkBasedConfigurationBuilder.New
            .WithBranch("main", b => b.WithIncrement(IncrementStrategy.Patch)
                .WithPreventIncrementWhenCurrentCommitTagged(preventIncrementWhenCurrentCommitTagged)
                .WithDeploymentMode(GitVersion.VersionCalculation.DeploymentMode.ContinuousDelivery)
            ).Build();

        // ✅ succeeds as expected
        fixture.AssertFullSemver(semanticVersion, configuration, commitId: commitA.Sha);
    }

    [Test]
    public void GivenGitHubFlowBasedWorkflowWithCommitParameterThenVersionShouldBeCorrect()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");
        var commitB = fixture.Repository.MakeACommit("B");
        fixture.MakeACommit("C");
        fixture.MakeACommit("D");

        var configuration = GitHubFlowConfigurationBuilder.New.Build();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-2", configuration, commitId: commitB.Sha);
    }

    [Test]
    public void GivenGitHubFlowWorkflowWithIgnoreConfigurationForCommitThenVersionShouldBeCorrect()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");
        var commitB = fixture.Repository.MakeACommit("B");
        fixture.MakeACommit("C");
        fixture.MakeACommit("D");

        var configuration = GitHubFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Shas = [commitB.Sha] })
            .Build();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-3", configuration);
    }

    [Test]
    public void GivenGitHubFlowWorkflowWithIgnoreConfigurationForCommitBAndCommitParameterAThenVersionShouldBeCorrect()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");
        var commitB = fixture.Repository.MakeACommit("B");
        var commitC = fixture.Repository.MakeACommit("C");
        fixture.MakeACommit("D");

        var configuration = GitHubFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Shas = [commitB.Sha] })
            .Build();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-2", configuration, commitId: commitC.Sha);
    }

    [Test]
    public void GivenGitHubFlowWorkflowWithIgnoreConfigurationForCommitCAndCommitParameterCThenCommitBShouldBeUsed()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");
        fixture.MakeACommit("B");
        var commitC = fixture.Repository.MakeACommit("C");

        var configuration = GitHubFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Shas = [commitC.Sha] })
            .Build();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-2", configuration, commitId: commitC.Sha);
    }

    [Test]
    public void GivenGitHubFlowWorkflowWithIgnoreConfigurationForTaggedCommitThenTagShouldBeIgnored()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");
        fixture.MakeACommit("B");
        var commitC = fixture.Repository.MakeACommit("C");
        fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("D");

        var configuration = GitHubFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Shas = [commitC.Sha] })
            .Build();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-3", configuration);
    }

    [Test]
    public void GivenGitHubFlowWorkflowWithIgnoreConfigurationBeforeCommitWithTagThenTagShouldBeIgnored()
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");
        fixture.MakeACommit("B");
        var commitC = fixture.Repository.MakeACommit("C");
        fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("D");

        var before = commitC.Committer.When.AddSeconds(1);
        var configuration = GitHubFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Before = before })
            .Build();

        // ✅ succeeds as expected
        fixture.AssertFullSemver("0.0.1-1", configuration);
    }

    [TestCase(false, "1.0.1-0")]
    [TestCase(true, "1.0.0")]
    public void GivenGitHubFlowWorkflowWithIgnoreConfigurationOfCommitBThenTagShouldBeConsidered(
        bool preventIncrementWhenCurrentCommitTagged, string semanticVersion)
    {
        using var fixture = new EmptyRepositoryFixture();

        fixture.MakeACommit("A");
        fixture.ApplyTag("1.0.0");
        var commitB = fixture.Repository.MakeACommit("B");

        var configuration = GitHubFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Shas = [commitB.Sha] })
            .WithBranch("main", b => b.WithPreventIncrementWhenCurrentCommitTagged(preventIncrementWhenCurrentCommitTagged))
            .Build();

        // ✅ succeeds as expected
        fixture.AssertFullSemver(semanticVersion, configuration);
    }

    [TestCase(false, "1.0.1-0")]
    [TestCase(true, "1.0.0")]
    public void GivenGitHubFlowWorkflowWithCommitParameterBThenTagShouldBeConsidered(
        bool preventIncrementWhenCurrentCommitTagged, string semanticVersion)
    {
        using var fixture = new EmptyRepositoryFixture();

        var commitA = fixture.Repository.MakeACommit("A");
        fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("B");

        var configuration = GitHubFlowConfigurationBuilder.New
            .WithBranch("main", b => b.WithPreventIncrementWhenCurrentCommitTagged(preventIncrementWhenCurrentCommitTagged))
            .Build();

        // ✅ succeeds as expected
        fixture.AssertFullSemver(semanticVersion, configuration, commitId: commitA.Sha);
    }

    [Test]
    public void GivenTrunkBasedWorkflowWithIgnoreConfigurationForPathThenVersionShouldBeCorrect()
    {
        using var fixture = new EmptyRepositoryFixture();

        var commitA = fixture.Repository.MakeACommit("A");
        var commitB = fixture.Repository.MakeACommit("B");
        fixture.MakeACommit("C");
        fixture.MakeACommit("D");

        var ignoredPath = fixture.Repository.Diff.Compare<LibGit2Sharp.TreeChanges>(commitA.Tree, commitB.Tree).Select(element => element.Path).First();

        var configuration = TrunkBasedConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Paths = { ignoredPath } })
            .Build();

        // commitB should be ignored, so version should be as if B didn't exist
        fixture.AssertFullSemver("0.0.3", configuration);
    }

    [TestCase(true, true, false, "0.2.0")]
    [TestCase(true, true, true, "0.2.0")]
    [TestCase(true, false, false, "0.2.0")]
    [TestCase(true, false, true, "0.2.0")]
    [TestCase(false, true, false, "0.1.0")]
    [TestCase(false, true, true, "0.1.0")]
    [TestCase(false, false, false, "0.1.0")]
    [TestCase(false, false, true, "0.1.0")]
    public void TrunkBasedFeatureMergePreservesMinorIncrementAfterIgnoredBase(
        bool includeFirstFeature, bool mergeDocs, bool ignoreBySha, string expectedVersion)
    {
        // Regression for #5501: an ignored branch point must not turn feature work into trunk/Patch commits.
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("Initial configuration");
        var ignore = new IgnoreConfiguration();
        if (!ignoreBySha)
        {
            ignore.Paths.Add("^docs/");
        }
        var configuration = TrunkBasedConfigurationBuilder.New.WithIgnoreConfiguration(ignore).Build();

        if (includeFirstFeature)
        {
            fixture.BranchTo("feature/first");
            fixture.MakeACommit("First feature");
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("feature/first");
            fixture.AssertFullSemver("0.1.0", configuration);
        }

        if (mergeDocs)
        {
            fixture.BranchTo("feature/docs");
        }
        var docsPath = Path.Combine(fixture.RepositoryPath, "docs", "readme.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(docsPath)!);
        File.WriteAllText(docsPath, "Documentation only");
        LibGit2Sharp.Commands.Stage(fixture.Repository, docsPath);
        var signature = Generate.SignatureNow();
        var docsCommit = fixture.Repository.Commit("Documentation only", signature, signature, new LibGit2Sharp.CommitOptions());
        if (mergeDocs)
        {
            fixture.Checkout(MainBranch);
            fixture.MergeNoFF("feature/docs");
        }
        var ignoredBase = fixture.Repository.Head.Tip;
        if (ignoreBySha)
        {
            ignore.Shas.UnionWith([docsCommit.Sha, ignoredBase.Sha]);
        }
        fixture.AssertFullSemver(includeFirstFeature ? "0.1.0" : "0.0.1", configuration);

        fixture.BranchTo("feature/second");
        fixture.MakeACommit("Second feature");
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/second");
        var merge = fixture.Repository.Head.Tip;
        var parents = merge.Parents.ToArray();
        fixture.Repository.ObjectDatabase.FindMergeBase(parents[0], parents[1]).ShouldNotBeNull().Sha.ShouldBe(ignoredBase.Sha);

        var version = fixture.GetVersion(configuration);

        version.FullSemVer.ShouldBe(expectedVersion);
        version.SemVerSourceIncrement.ShouldBe("Minor");
        version.Sha.ShouldBe(merge.Sha);
    }

    [Test]
    public void GivenTrunkBasedWorkflowWithIgnoreConfigurationForPathAndCommitParameterCThenVersionShouldBeCorrect()
    {
        using var fixture = new EmptyRepositoryFixture();

        var commitA = fixture.Repository.MakeACommit("A");
        fixture.MakeACommit("B");
        var commitC = fixture.Repository.MakeACommit("C");
        fixture.MakeACommit("D");

        var ignoredPath = fixture.Repository.Diff.Compare<LibGit2Sharp.TreeChanges>(commitA.Tree, commitC.Tree).Select(element => element.Path).First();

        var configuration = TrunkBasedConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Paths = { ignoredPath } })
            .Build();

        // commitC should be ignored, so version should be as if C didn't exist
        fixture.AssertFullSemver("0.0.2", configuration, commitId: commitC.Sha);
    }

    [Test]
    public void GivenGitHubFlowWorkflowWithIgnoreConfigurationForPathThenVersionShouldBeCorrect()
    {
        using var fixture = new EmptyRepositoryFixture();

        var commitA = fixture.Repository.MakeACommit("A");
        var commitB = fixture.Repository.MakeACommit("B");
        fixture.MakeACommit("C");
        fixture.MakeACommit("D");

        var ignoredPath = fixture.Repository.Diff.Compare<LibGit2Sharp.TreeChanges>(commitA.Tree, commitB.Tree).Select(element => element.Path).First();

        var configuration = GitHubFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Paths = { ignoredPath } })
            .Build();

        // commitB should be ignored, so version should be as if B didn't exist
        fixture.AssertFullSemver("0.0.1-3", configuration);
    }

    [Test]
    public void GivenTrunkBasedWorkflowWithIgnoreConfigurationForTaggedCommitPathThenTagShouldBeIgnored()
    {
        using var fixture = new EmptyRepositoryFixture();

        var commitA = fixture.Repository.MakeACommit("A");
        var commitB = fixture.Repository.MakeACommit("B");
        fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("C");

        var ignoredPath = fixture.Repository.Diff.Compare<LibGit2Sharp.TreeChanges>(commitA.Tree, commitB.Tree).Select(element => element.Path).First();

        var configuration = TrunkBasedConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Paths = { ignoredPath } })
            .Build();

        // commitB should be ignored, so version should be as if B didn't exist
        fixture.AssertFullSemver("0.0.2", configuration);
    }

    [Test]
    public void GivenGitHubFlowWorkflowWithIgnoreConfigurationForTaggedCommitPathThenTagShouldBeIgnored()
    {
        using var fixture = new EmptyRepositoryFixture();

        var commitA = fixture.Repository.MakeACommit("A");
        var commitB = fixture.Repository.MakeACommit("B");
        fixture.ApplyTag("1.0.0");
        fixture.MakeACommit("C");

        var ignoredPath = fixture.Repository.Diff.Compare<LibGit2Sharp.TreeChanges>(commitA.Tree, commitB.Tree).Select(element => element.Path).First();

        var configuration = GitHubFlowConfigurationBuilder.New
            .WithIgnoreConfiguration(new IgnoreConfiguration { Paths = { ignoredPath } })
            .Build();

        // commitB should be ignored, so version should be as if B didn't exist
        fixture.AssertFullSemver("0.0.1-2", configuration);
    }
}
