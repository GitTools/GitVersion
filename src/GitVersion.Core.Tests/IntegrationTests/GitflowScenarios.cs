using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;

namespace GitVersion.Core.Tests.IntegrationTests;

[TestFixture]
public class GitflowScenarios : TestBase
{
    [Test]
    public void GitflowComplexExample()
    {
        const string developBranch = "develop";
        const string feature1Branch = "feature/f1";
        const string feature2Branch = "feature/f2";
        const string release1Branch = "release/1.1.0";
        const string release2Branch = "release/1.2.0";
        const string hotfixBranch = "hotfix/hf";
        var configuration = GitFlowConfigurationBuilder.New.Build();

        using var fixture = new BaseGitFlowRepositoryFixture("1.0.0");
        fixture.AssertFullSemver("1.1.0-alpha.1", configuration);

        // Feature 1
        fixture.BranchTo(feature1Branch);
        fixture.MakeACommit("added feature 1");
        fixture.AssertFullSemver("1.1.0-f1.1+2", configuration);
        fixture.Checkout(developBranch);
        fixture.MergeNoFF(feature1Branch);
        fixture.Repository.Branches.Remove(fixture.Repository.Branches[feature1Branch]);
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
        fixture.MakeACommit("added feature 2");
        fixture.AssertFullSemver("1.2.0-f2.1+2", configuration);
        fixture.Checkout(developBranch);
        fixture.MergeNoFF(feature2Branch);
        fixture.Repository.Branches.Remove(fixture.Repository.Branches[feature2Branch]);
        fixture.AssertFullSemver("1.2.0-alpha.3", configuration);

        // Release 1.2.0
        fixture.BranchTo(release2Branch);
        fixture.MakeACommit("release stabilization");
        fixture.AssertFullSemver("1.2.0-beta.1+8", configuration);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF(release2Branch);
        fixture.AssertFullSemver("1.2.0-5", configuration);
        fixture.ApplyTag("1.2.0");
        fixture.AssertFullSemver("1.2.0", configuration);
        fixture.Checkout(developBranch);
        fixture.MergeNoFF(release2Branch);
        fixture.Repository.Branches.Remove(fixture.Repository.Branches[release2Branch]);
        fixture.AssertFullSemver("1.3.0-alpha.1", configuration);

        // Hotfix
        fixture.Checkout(MainBranch);
        fixture.BranchTo(hotfixBranch);
        fixture.MakeACommit("added hotfix");
        fixture.AssertFullSemver("1.2.1-beta.1+1", configuration);
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF(hotfixBranch);
        fixture.AssertFullSemver("1.2.1-2", configuration);
        fixture.ApplyTag("1.2.1");
        fixture.AssertFullSemver("1.2.1", configuration);
        fixture.Checkout(developBranch);
        fixture.MergeNoFF(hotfixBranch);
        fixture.Repository.Branches.Remove(fixture.Repository.Branches[hotfixBranch]);
        fixture.AssertFullSemver("1.3.0-alpha.2", configuration);
    }
}
