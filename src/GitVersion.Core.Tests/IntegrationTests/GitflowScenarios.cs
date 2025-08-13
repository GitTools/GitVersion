using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using GitVersion.Logging;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class GitflowScenarios : TestBase
{
    private readonly ILog log;

    public GitflowScenarios()
    {
        var sp = ConfigureServices();
        this.log = sp.GetRequiredService<ILog>();
    }

    [Test]
    public void GitflowComplexExample()
    {
        var keepBranches = true;
        const string developBranch = "develop";
        const string feature1Branch = "feature/f1";
        const string feature2Branch = "feature/f2";
        const string release1Branch = "release/1.1.0";
        const string release2Branch = "release/1.2.0";
        const string hotfixBranch = "hotfix/hf";

        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new BaseGitFlowRepositoryFixture(InitialMainAction, deleteOnDispose: false);
        var fullSemver = "1.1.0-alpha.1";
        fixture.AssertFullSemver(fullSemver, configuration);
        fixture.AssertCommitsSinceVersionSource(1, configuration);

        // Feature 1
        fixture.BranchTo(feature1Branch);

        fixture.MakeACommit($"added feature 1 >> {fullSemver}");
        fullSemver = "1.1.0-f1.1+2";
        fixture.AssertFullSemver(fullSemver, configuration);
        fixture.AssertCommitsSinceVersionSource(2, configuration);
        fixture.Checkout(developBranch);
        fixture.MergeNoFF(feature1Branch);
        if (!keepBranches) fixture.Repository.Branches.Remove(fixture.Repository.Branches[feature1Branch]);
        fixture.AssertFullSemver("1.1.0-alpha.3", configuration);
        fixture.AssertCommitsSinceVersionSource(3, configuration);

        // Release 1.1.0
        fixture.BranchTo(release1Branch);
        fixture.MakeACommit("release stabilization");
        fixture.AssertFullSemver("1.1.0-beta.1+4", configuration);
        fixture.AssertCommitsSinceVersionSource(4, configuration);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF(release1Branch);
        fixture.AssertFullSemver("1.1.0-5", configuration);
        fixture.AssertCommitsSinceVersionSource(5, configuration);
        fixture.ApplyTag("1.1.0");
        fixture.AssertFullSemver("1.1.0", configuration);
        fixture.AssertCommitsSinceVersionSource(0, configuration);
        fixture.Checkout(developBranch);
        fixture.MergeNoFF(release1Branch);
        fixture.Repository.Branches.Remove(fixture.Repository.Branches[release1Branch]);
        fixture.AssertFullSemver("1.2.0-alpha.1", configuration);
        fixture.AssertCommitsSinceVersionSource(1, configuration);

        // Feature 2
        fixture.BranchTo(feature2Branch);
        fullSemver = "1.2.0-f2.1+2";
        fixture.MakeACommit($"added feature 2 >> {fullSemver}");
        fixture.AssertFullSemver(fullSemver, configuration);
        fixture.AssertCommitsSinceVersionSource(2, configuration);
        fixture.Checkout(developBranch);
        fixture.MergeNoFF(feature2Branch);
        if (!keepBranches) fixture.Repository.Branches.Remove(fixture.Repository.Branches[feature2Branch]);
        fixture.AssertFullSemver("1.2.0-alpha.3", configuration);
        fixture.AssertCommitsSinceVersionSource(3, configuration);

        // Release 1.2.0
        fixture.BranchTo(release2Branch);
        fullSemver = "1.2.0-beta.1+8";
        fixture.MakeACommit($"release stabilization >> {fullSemver}");
        fixture.AssertFullSemver(fullSemver, configuration);
        fixture.AssertCommitsSinceVersionSource(8, configuration);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF(release2Branch);
        fixture.AssertFullSemver("1.2.0-5", configuration);
        fixture.AssertCommitsSinceVersionSource(5, configuration);
        fixture.ApplyTag("1.2.0");
        fixture.AssertFullSemver("1.2.0", configuration);
        fixture.AssertCommitsSinceVersionSource(0, configuration);
        fixture.Checkout(developBranch);
        fixture.MergeNoFF(release2Branch);
        if (!keepBranches)
        {
            fixture.Repository.Branches.Remove(fixture.Repository.Branches[release2Branch]);
        }
        fixture.AssertFullSemver("1.3.0-alpha.1", configuration);
        fixture.AssertCommitsSinceVersionSource(1, configuration);

        // Hotfix
        fixture.Checkout(MainBranch);
        fixture.BranchTo(hotfixBranch);
        fullSemver = "1.2.1-beta.1+1";
        fixture.MakeACommit($"added hotfix >> {fullSemver}");
        fixture.AssertFullSemver(fullSemver, configuration);
        fixture.AssertCommitsSinceVersionSource(1, configuration);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF(hotfixBranch);
        fixture.AssertFullSemver("1.2.1-2", configuration);
        fixture.AssertCommitsSinceVersionSource(2, configuration);
        fixture.ApplyTag("1.2.1");
        fixture.AssertFullSemver("1.2.1", configuration);
        fixture.AssertCommitsSinceVersionSource(0, configuration);
        fixture.Checkout(developBranch);
        fixture.MergeNoFF(hotfixBranch);
        if (!keepBranches)
        {
            fixture.Repository.Branches.Remove(fixture.Repository.Branches[hotfixBranch]);
        }
        fixture.AssertFullSemver("1.3.0-alpha.2", configuration);
        fixture.AssertCommitsSinceVersionSource(2, configuration);

        fixture.Checkout(feature2Branch);
        fixture.SequenceDiagram.NoteOver($"Checkout {feature2Branch}", feature2Branch);
        fixture.AssertFullSemver("1.3.0-f2.1+0", configuration);
        fixture.SequenceDiagram.NoteOver(
        string.Join(System.Environment.NewLine, ("Feature branches are configured to inherit version (increment: inherit)." + System.Environment.NewLine + System.Environment.NewLine +
        "GitVersion uses the merge base between the feature and develop to determine the version." + System.Environment.NewLine + System.Environment.NewLine +
        "As develop progresses (e.g., by releasing 1.2.0 & 1.2.1), rebuilding old feature branches can produce different versions." + System.Environment.NewLine + System.Environment.NewLine +
        "Here we've checked out commit H again and now it's it's own VersionSource and produces 1.3.0-f2.1+0").SplitIntoLines(60)), feature2Branch);
        fixture.AssertCommitsSinceVersionSource(0, configuration);

        fullSemver = "1.3.0-f2.1+1";
        fixture.MakeACommit(
            "feature 2 additional commit after original feature has been merged to develop " + System.Environment.NewLine +
            $"and release/1.2.0 has already happened >> {fullSemver}"
        );
        fixture.AssertFullSemver(fullSemver, configuration);
        fixture.AssertCommitsSinceVersionSource(1, configuration);
        fixture.SequenceDiagram.NoteOver(
        string.Join(System.Environment.NewLine, ($"We committed again to {feature2Branch}." + System.Environment.NewLine + System.Environment.NewLine +
        "Why is the VersionSource no longer H but has instead jumped to N?" + System.Environment.NewLine + System.Environment.NewLine +
        $"I expected this to produce {fullSemver} and it does.").SplitIntoLines(60)), feature2Branch);

        var gitRepository = fixture.Repository.ToGitRepository();
        var gitRepoMetadataProvider = new RepositoryStore(this.log, gitRepository);
        // H can't be it's own ancestor, so merge base is G
        fixture.SequenceDiagram.GetOrAddLabel(gitRepoMetadataProvider.FindMergeBase(gitRepository.Branches[feature2Branch], gitRepository.Branches[developBranch]).Sha).ShouldBe("G");
        fixture.SequenceDiagram.GetOrAddLabel(gitRepoMetadataProvider.FindMergeBase(gitRepository.Branches[feature2Branch], gitRepository.Branches[MainBranch]).Sha).ShouldBe("G");
        // Why is H it's own VersionSource though if after committing with H as the ancestor we get N as the VersionSource?

        fixture.SequenceDiagram.NoteOver($"Now we rebase {feature2Branch} onto {developBranch}", feature2Branch);

        var identity = new Identity(
                fixture.Repository.Head.Tip.Committer.Name,
                fixture.Repository.Head.Tip.Committer.Email);
        var rebaseResult = fixture.Repository.Rebase.Start(
            fixture.Repository.Branches[feature2Branch],
            fixture.Repository.Branches[developBranch],
            fixture.Repository.Branches[developBranch],
            identity,
            new RebaseOptions());
        while (rebaseResult != null && rebaseResult.Status != RebaseStatus.Complete)
        {
            rebaseResult = fixture.Repository.Rebase.Continue(identity, new RebaseOptions());
        }

        fullSemver = "1.3.0-f2.1+3";
        fixture.AssertFullSemver(fullSemver, configuration);
        fixture.AssertCommitsSinceVersionSource(3, configuration);
        fixture.SequenceDiagram.NoteOver(
        string.Join(System.Environment.NewLine, $"Post rebase the VersionSource is again N - the last commit on {MainBranch}." + System.Environment.NewLine + System.Environment.NewLine +
                $"I expected this to produce 1.3.0-f2.1+1 and have a VersionSource of O with self as one commit since VersionSource. Instead VersionSource of N produces {fullSemver}, with a count traversal that includes both L and O!".SplitIntoLines(60)), feature2Branch);

        void InitialMainAction(IRepository r)
        {
            if (configuration is GitVersionConfiguration concreteConfig)
            {
                var yaml = new ConfigurationSerializer().Serialize(concreteConfig);
                const string fileName = "GitVersion.yml";
                var filePath = FileSystemHelper.Path.Combine(r.Info.Path, "..", fileName);
                File.WriteAllText(filePath, yaml);
                r.Index.Add(fileName);
                r.Index.Write();
            }

            r.MakeATaggedCommit("1.0.0", $"Initial commit on {MainBranch}");
        }
    }
}
