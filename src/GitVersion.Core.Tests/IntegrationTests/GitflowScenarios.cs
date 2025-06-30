using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class GitflowScenarios : TestBase
{
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

        using var fixture = new BaseGitFlowRepositoryFixture(initialMainAction, deleteOnDispose: false);
        var fullSemver = "1.1.0-alpha.1";
        fixture.AssertFullSemver(fullSemver, configuration);

        // Feature 1
        fixture.BranchTo(feature1Branch);

        fixture.MakeACommit($"added feature 1 >> {fullSemver}");
        fullSemver = "1.1.0-f1.1+2";
        fixture.AssertFullSemver(fullSemver, configuration);
        fixture.Checkout(developBranch);
        fixture.MergeNoFF(feature1Branch);
        if (!keepBranches) fixture.Repository.Branches.Remove(fixture.Repository.Branches[feature1Branch]);
        fixture.AssertFullSemver("1.1.0-alpha.3", configuration);

        // Release 1.1.0
        fixture.BranchTo(release1Branch);
        fixture.MakeACommit("release stabilization");
        fixture.AssertFullSemver("1.1.0-beta.1+4", configuration);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF(release1Branch);
        fixture.AssertFullSemver("1.1.0-5", configuration);
        fixture.ApplyTag("1.1.0");
        fixture.AssertFullSemver("1.1.0", configuration);
        fixture.Checkout(developBranch);
        fixture.MergeNoFF(release1Branch);
        fixture.Repository.Branches.Remove(fixture.Repository.Branches[release1Branch]);
        fixture.AssertFullSemver("1.2.0-alpha.1", configuration);

        // Feature 2
        fixture.BranchTo(feature2Branch);
        fullSemver = "1.2.0-f2.1+2";
        fixture.MakeACommit($"added feature 2 >> {fullSemver}");
        fixture.AssertFullSemver(fullSemver, configuration);
        fixture.Checkout(developBranch);
        fixture.MergeNoFF(feature2Branch);
        if (!keepBranches) fixture.Repository.Branches.Remove(fixture.Repository.Branches[feature2Branch]);
        fixture.AssertFullSemver("1.2.0-alpha.3", configuration);

        // Release 1.2.0
        fixture.BranchTo(release2Branch);
        fullSemver = "1.2.0-beta.1+8";
        fixture.MakeACommit($"release stabilization >> {fullSemver}");
        fixture.AssertFullSemver(fullSemver, configuration);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF(release2Branch);
        fixture.AssertFullSemver("1.2.0-5", configuration);
        fixture.ApplyTag("1.2.0");
        fixture.AssertFullSemver("1.2.0", configuration);
        fixture.Checkout(developBranch);
        fixture.MergeNoFF(release2Branch);
        if (!keepBranches)
        {
            fixture.Repository.Branches.Remove(fixture.Repository.Branches[release2Branch]);
        }
        fixture.AssertFullSemver("1.3.0-alpha.1", configuration);

        // Hotfix
        fixture.Checkout(MainBranch);
        fixture.BranchTo(hotfixBranch);
        fullSemver = "1.2.1-beta.1+1";
        fixture.MakeACommit($"added hotfix >> {fullSemver}");
        fixture.AssertFullSemver(fullSemver, configuration);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF(hotfixBranch);
        fixture.AssertFullSemver("1.2.1-2", configuration);
        fixture.ApplyTag("1.2.1");
        fixture.AssertFullSemver("1.2.1", configuration);
        fixture.AssertCommitsSinceVersionSource(2, configuration);
        fixture.Checkout(developBranch);
        fixture.MergeNoFF(hotfixBranch);
        if (!keepBranches)
        {
            fixture.Repository.Branches.Remove(fixture.Repository.Branches[hotfixBranch]);
        }
        fixture.AssertFullSemver("1.3.0-alpha.2", configuration);

        fixture.Checkout(feature2Branch);
        fixture.AssertFullSemver(
            "1.3.0-f2.1+0",
            configuration,
            customMessage:
        "Feature branches use inherited versioning (increment: inherit), " + System.Environment.NewLine +
        "and your config inherits from develop." + System.Environment.NewLine + System.Environment.NewLine +
        "GitVersion uses the merge base between the feature and develop to determine the version." + System.Environment.NewLine + System.Environment.NewLine +
        "As develop progresses (e.g., by releasing 1.2.0), rebuilding old feature branches can" + System.Environment.NewLine +
        "produce different versions.");

        fullSemver = "1.3.0-f2.1+1";
        fixture.MakeACommit(
            "feature 2 additional commit after original feature has been merged to develop " + System.Environment.NewLine +
            $"and release/1.2.0 has already happened >> {fullSemver}" +
            "Problem #1: 1.3.0-f2.1+0 is what I observe when I run dotnet-gitversion 6.3.0 but in the repo the assertion is 1.3.0-f2.1+1" +
            "After rebase 1.3.0-f2.1+3 is both what the test asserts and what I observe when I run dotnet-gitversion 6.3.0." +
            "Problem #2: I expected to get the same before and after the rebase." +
            "" +
            "Whether my expectations are correct or not could we at least build upon the documentation I have started to add " +
            "as an explanation of observed behaviour. I'm happy to translate an explanation in to test " +
            "documentation if you confirm it would be accepted on PR."
        );

        var identity = new Identity(
                fixture.Repository.Head.Tip.Committer.Name,
                fixture.Repository.Head.Tip.Committer.Email);
        fixture.AssertFullSemver(fullSemver, configuration);
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

        fixture.AssertFullSemver(fullSemver, configuration, customMessage: "I expected to get the same before and after the rebase.");

        void initialMainAction(IRepository r)
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
