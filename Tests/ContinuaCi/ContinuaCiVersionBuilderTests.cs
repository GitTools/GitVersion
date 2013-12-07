using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class ContinuaCiVersionBuilderTests
{

    [Test]
    public void Develop_branch()
    {
        var versionAndBranch = new VersionAndBranch
                               {
                                   BranchType = BranchType.Develop,
                                   Version = new SemanticVersion
                                             {
                                                 PreReleasePartOne = 4,
                                                 Stability = Stability.Unstable
                                             }
                               };

        var versionBuilder = new ContinuaCiVersionBuilder();
        var continuaCiVersion = versionBuilder.GenerateBuildVersion(versionAndBranch);
        Assert.AreEqual("@@continua[setBuildVersion value='0.0.0-Unstable4']", continuaCiVersion);
    }

    [Test]
    public void Develop_branch_with_preReleaseTwo()
    {
        var versionAndBranch = new VersionAndBranch
                               {
                                   BranchType = BranchType.Develop,
                                   Version = new SemanticVersion
                                             {
                                                 PreReleasePartOne = 4,
                                                 PreReleasePartTwo = 6,
                                                 Stability = Stability.Unstable
                                             }
                               };

        var versionBuilder = new ContinuaCiVersionBuilder();
        var continuaCiVersion = versionBuilder.GenerateBuildVersion(versionAndBranch);
        Assert.AreEqual("@@continua[setBuildVersion value='0.0.0-Unstable4.6']", continuaCiVersion);
    }

    [Test]
    public void Release_branch()
    {
        var versionAndBranch = new VersionAndBranch
                               {
                                   BranchType = BranchType.Release,
                                   Version = new SemanticVersion
                                             {
                                                 PreReleasePartOne = 4,
                                                 Stability = Stability.Beta,
                                             }
                               };

        var versionBuilder = new ContinuaCiVersionBuilder();
        var continuaCiVersion = versionBuilder.GenerateBuildVersion(versionAndBranch);
        Assert.AreEqual("@@continua[setBuildVersion value='0.0.0-Beta4']", continuaCiVersion);
    }

    [Test]
    public void Release_branch_with_preReleaseTwo()
    {
        var versionAndBranch = new VersionAndBranch
                               {
                                   BranchType = BranchType.Release,
                                   Version = new SemanticVersion
                                             {
                                                 PreReleasePartOne = 4,
                                                 PreReleasePartTwo = 8,
                                                 Stability = Stability.Beta,
                                             }
                               };

        var versionBuilder = new ContinuaCiVersionBuilder();
        var continuaCiVersion = versionBuilder.GenerateBuildVersion(versionAndBranch);
        Assert.AreEqual("@@continua[setBuildVersion value='0.0.0-Beta4.8']", continuaCiVersion);
    }

    [Test]
    public void Hotfix_branch()
    {
        var versionAndBranch = new VersionAndBranch
                               {
                                   BranchType = BranchType.Hotfix,
                                   Version = new SemanticVersion
                                             {
                                                 Stability = Stability.Beta,
                                                 PreReleasePartOne = 4
                                             }
                               };

        var versionBuilder = new ContinuaCiVersionBuilder();
        var continuaCiVersion = versionBuilder.GenerateBuildVersion(versionAndBranch);
        Assert.AreEqual("@@continua[setBuildVersion value='0.0.0-Beta4']", continuaCiVersion);
    }

    [Test]
    public void Hotfix_branch_with_preReleaseTwo()
    {
        var versionAndBranch = new VersionAndBranch
                               {
                                   BranchType = BranchType.Hotfix,
                                   Version = new SemanticVersion
                                             {
                                                 Stability = Stability.Beta,
                                                 PreReleasePartOne = 4,
                                                 PreReleasePartTwo = 7,
                                             }
                               };

        var versionBuilder = new ContinuaCiVersionBuilder();
        var continuaCiVersion = versionBuilder.GenerateBuildVersion(versionAndBranch);
        Assert.AreEqual("@@continua[setBuildVersion value='0.0.0-Beta4.7']", continuaCiVersion);
    }

    [Test]
    public void Pull_branch()
    {
        var versionAndBranch = new VersionAndBranch
                               {
                                   BranchType = BranchType.PullRequest,
                                   Version = new SemanticVersion
                                             {
                                                 Suffix = "1571",
                                                 PreReleasePartOne = 131231232, //ignored
                                                 PreReleasePartTwo = 131231232, //ignored
                                                 Stability = Stability.Unstable
                                             }
                               };

        var versionBuilder = new ContinuaCiVersionBuilder();
        var continuaCiVersion = versionBuilder.GenerateBuildVersion(versionAndBranch);
        Assert.AreEqual("@@continua[setBuildVersion value='0.0.0-PullRequest-1571']", continuaCiVersion);
    }

    [Test]
    public void Feature_branch()
    {
        var versionAndBranch = new VersionAndBranch
                               {
                                   BranchType = BranchType.Feature,
                                   Sha = "TheSha",
                                   BranchName = "AFeature",
                                   Version = new SemanticVersion
                                             {
                                                 PreReleasePartOne = 4, //ignored
                                                 PreReleasePartTwo = 4, //ignored
                                                 Stability = Stability.Unstable
                                             }
                               };

        var versionBuilder = new ContinuaCiVersionBuilder();
        var continuaCiVersion = versionBuilder.GenerateBuildVersion(versionAndBranch);
        Assert.AreEqual("@@continua[setBuildVersion value='0.0.0-Feature-AFeature-TheSha']", continuaCiVersion);
    }

    [Test]
    public void Master_branch()
    {
        var versionAndBranch = new VersionAndBranch
                               {
                                   Version = new SemanticVersion
                                             {
                                                 Stability = Stability.Final,
                                                 Suffix = "1571", //ignored
                                                 PreReleasePartOne = 131231232, //ignored
                                                 PreReleasePartTwo = 131231232 //ignored
                                             }
                               };

        var versionBuilder = new ContinuaCiVersionBuilder();
        var continuaCiVersion = versionBuilder.GenerateBuildVersion(versionAndBranch);
        Assert.AreEqual("@@continua[setBuildVersion value='0.0.0']", continuaCiVersion);
    }
}