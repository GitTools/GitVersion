using GitVersion;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class FeatureBranchScenarios
{
    [Test]
    public void ShouldInheritIncrementCorrectlyWithMultiplePossibleParentsAndWeirdlyNamedDevelopBranch()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("development");
            fixture.Repository.Checkout("development");

            //Create an initial feature branch
            var feature123 = fixture.Repository.CreateBranch("feature/JIRA-123");
            fixture.Repository.Checkout("feature/JIRA-123");
            fixture.Repository.MakeCommits(1);

            //Merge it
            fixture.Repository.Checkout("development");
            fixture.Repository.Merge(feature123, Constants.SignatureNow());

            //Create a second feature branch
            fixture.Repository.CreateBranch("feature/JIRA-124");
            fixture.Repository.Checkout("feature/JIRA-124");
            fixture.Repository.MakeCommits(1);

            fixture.AssertFullSemver("1.1.0-JIRA-124.1+2");
        }
    }
    
    [Test]
    public void BranchCreatedAfterFastForwardMergeShouldInheritCorrectly()
    {
        var config = new Config
        {
            Branches =
            {
                { "unstable", new BranchConfig { Increment = IncrementStrategy.Minor } }
            }
        };

        using (var fixture = new EmptyRepositoryFixture(config))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("unstable");
            fixture.Repository.Checkout("unstable");

            //Create an initial feature branch
            var feature123 = fixture.Repository.CreateBranch("feature/JIRA-123");
            fixture.Repository.Checkout("feature/JIRA-123");
            fixture.Repository.MakeCommits(1);

            //Merge it
            fixture.Repository.Checkout("unstable");
            fixture.Repository.Merge(feature123, Constants.SignatureNow());

            //Create a second feature branch
            fixture.Repository.CreateBranch("feature/JIRA-124");
            fixture.Repository.Checkout("feature/JIRA-124");
            fixture.Repository.MakeCommits(1);

            fixture.AssertFullSemver("1.1.0-JIRA-124.1+2");
        }
    }

    [Test]
    public void ShouldNotUseNumberInFeatureBranchAsPreReleaseNumberOffDevelop()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.Checkout("develop");
            fixture.Repository.CreateBranch("feature/JIRA-123");
            fixture.Repository.Checkout("feature/JIRA-123");
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver("1.1.0-JIRA-123.1+5");
        }
    }

    [Test]
    public void ShouldNotUseNumberInFeatureBranchAsPreReleaseNumberOffMaster()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("feature/JIRA-123");
            fixture.Repository.Checkout("feature/JIRA-123");
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver("1.0.1-JIRA-123.1+5");
        }
    }

    [Test]
    public void TestFeatureBranch()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("feature-test");
            fixture.Repository.Checkout("feature-test");
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver("1.0.1-test.1+5");
        }
    }

    [Test]
    public void TestFeaturesBranch()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("features/test");
            fixture.Repository.Checkout("features/test");
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver("1.0.1-test.1+5");
        }
    }

    [Test]
    public void WhenTwoFeatureBranchPointToTheSameCommit()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("develop");
            fixture.Repository.Checkout("develop");
            fixture.Repository.CreateBranch("feature/feature1");
            fixture.Repository.Checkout("feature/feature1");
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("feature/feature2");
            fixture.Repository.Checkout("feature/feature2");

            fixture.AssertFullSemver("0.1.0-feature2.1+1");
        }
    }

    [Test]
    public void ShouldBePossibleToMergeDevelopForALongRunningBranchWhereDevelopAndMasterAreEqual()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config() { VersioningMode = VersioningMode.ContinuousDeployment }))
        {
            fixture.Repository.MakeATaggedCommit("v1.0.0");

            fixture.Repository.CreateBranch("develop");
            fixture.Repository.Checkout("develop");

            fixture.Repository.CreateBranch("feature/longrunning");
            fixture.Repository.Checkout("feature/longrunning");
            fixture.Repository.MakeACommit();

            fixture.Repository.Checkout("develop");
            fixture.Repository.MakeACommit();

            fixture.Repository.Checkout("master");
            fixture.Repository.Merge(fixture.Repository.FindBranch("develop"), Constants.SignatureNow());
            fixture.Repository.ApplyTag("v1.1.0");

            fixture.Repository.Checkout("feature/longrunning"); 
            fixture.Repository.Merge(fixture.Repository.FindBranch("develop"), Constants.SignatureNow());

            fixture.AssertFullSemver("1.2.0-longrunning.2");
        }
    }

    [Test]
    public void FeatureBranchShouldTrackDevelopBranchIfPossible()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config() { VersioningMode = VersioningMode.ContinuousDeployment }))
        {
            fixture.Repository.MakeATaggedCommit("v1.0.0");

            fixture.Repository.CreateBranch("develop");
            fixture.Repository.Checkout("develop");

            fixture.Repository.CreateBranch("feature/a-feature");
            fixture.Repository.Checkout("feature/a-feature");
            fixture.Repository.MakeACommit();

            fixture.Repository.Checkout("develop");
            fixture.Repository.MergeNoFF("feature/a-feature");

            fixture.Repository.Checkout("master");
            // NOTE: develop and master will diverge here when running git flow
            fixture.MergeNoFF("develop");
            // NOTE: if we're merging develop to master with fast forward, the test will pass
            // fixture.Repository.Merge(fixture.Repository.FindBranch("develop"), Constants.SignatureNow(), new MergeOptions() {FastForwardStrategy = FastForwardStrategy.FastForwardOnly});

            fixture.Repository.ApplyTag("v2.0.0");

            fixture.Repository.Checkout("develop");
            fixture.Repository.CreateBranch("feature/snd-feature");
            fixture.Repository.Checkout("feature/snd-feature");
            fixture.Repository.MakeACommit();

            fixture.Repository.Checkout("develop");
            fixture.Repository.MakeACommit();

            fixture.Repository.DumpGraph();

            // NOTE: develop behaves correctly
            fixture.AssertFullSemver("2.1.0-unstable.1");

            fixture.Checkout("feature/snd-feature");
            // NOTE: I would expect something like the assertion below
            fixture.AssertFullSemver("2.1.0-snd-feature.1");
            // BUT WAS: "1.1.0-snd-feature.3"
        }
    }
}