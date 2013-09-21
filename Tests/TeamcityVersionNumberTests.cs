using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class TeamcityVersionNumberTests
{

    [Test]
    public void Develop_branch()
    {
        VerifyOutput("0.0.0-Unstable4",
            new VersionAndBranch
            {
                BranchType = BranchType.Develop,
                Version = new SemanticVersion
                                     {
                                         PreReleaseNumber = 4
                                     }
            });

    }


    [Test]
    public void Release_branch()
    {
        VerifyOutput("0.0.0-Beta4",
            new VersionAndBranch
            {
                BranchType = BranchType.Release,
                Version = new SemanticVersion
                                     {
                                         PreReleaseNumber = 4,
                                         Stability = Stability.Beta,
                                     }
            });

    }

    [Test]
    public void Hotfix_branch()
    {
        VerifyOutput("0.0.0-Beta4",
            new VersionAndBranch
            {
                BranchType = BranchType.Hotfix,
                Version = new SemanticVersion
                                     {
                                         Stability = Stability.Beta,
                                         PreReleaseNumber = 4
                                     }
            });

    }


    [Test]
    public void Pull_branch()
    {
        VerifyOutput("0.0.0-PullRequest-1571",
            new VersionAndBranch
            {
                BranchType = BranchType.PullRequest,
                Version = new SemanticVersion
                                     {
                                         Suffix = "1571",
                                         PreReleaseNumber = 131231232 //ignored
                                     }

            });

    }

    [Test]
    public void Feature_branch()
    {
        VerifyOutput("0.0.0-Feature-AFeature-THESHA",
            new VersionAndBranch
            {
                BranchType = BranchType.Feature,
                Sha = "THESHA",
                BranchName = "AFeature",
                Version = new SemanticVersion
                                     {
                                         PreReleaseNumber = 4 //ignored
                                     }
            });

    }


    [Test]
    public void Master_branch()
    {
        VerifyOutput("0.0.0",
            new VersionAndBranch
            {
                Version = new SemanticVersion
                                     {
                                         Stability = Stability.Final,
                                         Suffix = "1571", //ignored
                                         PreReleaseNumber = 131231232 //ignored
                                     }
            });

    }


    void VerifyOutput(string versionString, VersionAndBranch version)
    {
        var tcVersion = TeamCity.GenerateBuildVersion(version);

        Assert.True(TeamCity.GenerateBuildVersion(version).Contains(versionString), string.Format("TeamCity string {0} did not match expected string {1}", tcVersion, versionString));
    }

}