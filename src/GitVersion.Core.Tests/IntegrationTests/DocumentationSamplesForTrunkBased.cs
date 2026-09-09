using GitVersion.Configuration;

namespace GitVersion.Tests.IntegrationTests;

[TestFixture]
public class DocumentationSamplesForTrunkBased
{
    [Test]
    public void DirectCommitsAndReleaseTags()
    {
        var configuration = TrunkBasedConfigurationBuilder.New.Build();
        using var fixture = new EmptyRepositoryFixture();
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.ApplyTag("1.2.0");
        fixture.AssertFullSemver("1.2.0", configuration);

        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.1", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.2", configuration);
        fixture.ApplyTag("v1.2.2");
        fixture.AssertFullSemver("1.2.2", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.2.3", configuration);

        DocumentationDiagramWriter.Write(fixture.SequenceDiagram,
            $"{nameof(DocumentationSamplesForTrunkBased)}_{nameof(DirectCommitsAndReleaseTags)}", true);
    }

    [TestCase("feature/foo", "1.3.0-foo.1", "1.3.0-foo.2", "1.3.0", "1.3.1")]
    [TestCase("hotfix/fix", "1.2.1-fix.1", "1.2.1-fix.2", "1.2.1", "1.2.2")]
    public void BranchMerge(string branchName, string firstCommitVersion, string secondCommitVersion,
        string mergedVersion, string nextVersion)
    {
        var configuration = TrunkBasedConfigurationBuilder.New.Build();
        using var fixture = new EmptyRepositoryFixture();
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.ApplyTag("1.2.0");
        fixture.AssertFullSemver("1.2.0", configuration);

        fixture.BranchTo(branchName);
        fixture.SequenceDiagram.Deactivate("main");
        fixture.SequenceDiagram.Activate(branchName);
        fixture.MakeACommit();
        fixture.AssertFullSemver(firstCommitVersion, configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver(secondCommitVersion, configuration);

        fixture.Checkout("main");
        fixture.SequenceDiagram.Activate("main");
        fixture.MergeNoFF(branchName);
        fixture.SequenceDiagram.Deactivate(branchName);
        fixture.Remove(branchName);
        fixture.AssertFullSemver(mergedVersion, configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver(nextVersion, configuration);

        DocumentationDiagramWriter.Write(fixture.SequenceDiagram,
            $"{nameof(DocumentationSamplesForTrunkBased)}_{branchName.Split('/')[0]}_{nameof(BranchMerge)}", true);
    }

    [Test]
    public void CommitMessageIncrements()
    {
        var configuration = TrunkBasedConfigurationBuilder.New.Build();
        using var fixture = new EmptyRepositoryFixture();
        fixture.SequenceDiagram.Activate("main");
        fixture.MakeACommit();
        fixture.ApplyTag("1.2.0");
        fixture.AssertFullSemver("1.2.0", configuration);

        fixture.MakeACommit("Fix a bug +semver: patch");
        fixture.AssertFullSemver("1.2.1", configuration);
        fixture.MakeACommit("Add an API +semver: minor");
        fixture.AssertFullSemver("1.3.0", configuration);
        fixture.MakeACommit("Break an API +semver: major");
        fixture.AssertFullSemver("2.0.0", configuration);
        fixture.MakeACommit();
        fixture.AssertFullSemver("2.0.1", configuration);

        DocumentationDiagramWriter.Write(fixture.SequenceDiagram,
            $"{nameof(DocumentationSamplesForTrunkBased)}_{nameof(CommitMessageIncrements)}", true);
    }
}
