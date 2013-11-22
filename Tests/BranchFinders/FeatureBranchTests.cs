using FluentDate;
using FluentDateTimeOffset;
using GitFlowVersion;
using NUnit.Framework;

[TestFixture]
public class FeatureBranchTests
{
    [Test]
    public void Feature_branch_with_no_commit()
    {
        //this scenario should redirect to the develop finder since there is no diff btw this branch and the develop branch
        var branchingCommit = new MockCommit
                              {
                                  CommitterEx = 1.Seconds().Ago().ToSignature(),
                              };
        var featureBranch = new MockBranch("featureWithNoCommits")
                            {
                                branchingCommit
                            };
        var finder = new FeatureVersionFinder
                     {
                         Repository = new MockRepository
                                      {
                                          Branches = new MockBranchCollection
                                                     {
                                                         new MockBranch("master")
                                                         {
                                                             new MockMergeCommit
                                                             {
                                                                 MessageEx = "Merge branch 'release-0.2.0'",
                                                                 CommitterEx = 3.Seconds().Ago().ToSignature()
                                                             }
                                                         },
                                                         featureBranch,
                                                         new MockBranch("develop")
                                                         {
                                                             branchingCommit,
                                                             new MockCommit
                                                             {
                                                                 CommitterEx = 2.Seconds().Ago().ToSignature()
                                                             }
                                                         }
                                                     }
                                      },
                         Commit = branchingCommit,
                         FeatureBranch = featureBranch,
                         FindFirstCommitOnBranchFunc = () => branchingCommit.Id
                     };
        var version = finder.FindVersion();

        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(3, version.Version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Version.Stability);
        Assert.AreEqual(BranchType.Feature, version.BranchType);
        Assert.AreEqual(null, version.Version.Suffix);
        Assert.AreEqual(2, version.Version.PreReleasePartOne, "Should be the number of commits ahead of master");
    }

    [Test]
    public void Feature_branch_with_1_commit()
    {
        var branchingCommit = new MockCommit
                              {
                                  CommitterEx = 1.Seconds().Ago().ToSignature(),
                              };
        var commitOneOnFeature = new MockCommit
                                 {
                                     CommitterEx = 1.Seconds().Ago().ToSignature(),
                                 };
        var featureBranch = new MockBranch("featureWithNoCommits")
                            {
                                branchingCommit,
                                commitOneOnFeature
                            };

        var finder = new FeatureVersionFinder
                     {
                         Repository = new MockRepository
                                      {
                                          Branches = new MockBranchCollection
                                                     {
                                                         new MockBranch("master")
                                                         {
                                                             new MockMergeCommit
                                                             {
                                                                 MessageEx = "Merge branch 'release-0.2.0'",
                                                                 CommitterEx = 3.Seconds().Ago().ToSignature()
                                                             }
                                                         },
                                                         featureBranch,
                                                         new MockBranch("develop")
                                                         {
                                                             branchingCommit,
                                                             new MockCommit
                                                             {
                                                                 CommitterEx = 2.Seconds().Ago().ToSignature()
                                                             }
                                                         }
                                                     }
                                      },
                         Commit = commitOneOnFeature,
                         FeatureBranch = featureBranch,
                         FindFirstCommitOnBranchFunc = () => branchingCommit.Id
                     };
        var version = finder.FindVersion();
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(3, version.Version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Version.Stability);
        Assert.AreEqual(BranchType.Feature, version.BranchType);
        Assert.AreEqual(branchingCommit.Prefix(), version.Version.Suffix, "Suffix should be the develop commit it was branched from");
        Assert.AreEqual(0, version.Version.PreReleasePartOne, "Prerelease is always 0 for feature branches");
    }

    [Test]
    public void Feature_branch_with_2_commits()
    {

        var branchingCommit = new MockCommit
                              {
                                  CommitterEx = 3.Seconds().Ago().ToSignature(),
                              };
        var commitOneOnFeature = new MockCommit
                                 {
                                     CommitterEx = 2.Seconds().Ago().ToSignature(),
                                 };
        var commitTwoOnFeature = new MockCommit
                                 {
                                     CommitterEx = 1.Seconds().Ago().ToSignature(),
                                 };
        var featureBranch = new MockBranch("featureWithNoCommits")
                            {
                                branchingCommit,
                                commitOneOnFeature,
                                commitTwoOnFeature,
                            };
        var finder = new FeatureVersionFinder
                     {
                         Repository = new MockRepository
                                      {
                                          Branches = new MockBranchCollection
                                                     {
                                                         new MockBranch("master")
                                                         {
                                                             new MockMergeCommit
                                                             {
                                                                 MessageEx = "Merge branch 'release-0.2.0'",
                                                                 CommitterEx = 4.Seconds().Ago().ToSignature()
                                                             }
                                                         },
                                                         featureBranch,
                                                         new MockBranch("develop")
                                                         {
                                                             branchingCommit,
                                                             new MockCommit
                                                             {
                                                                 CommitterEx = 2.Seconds().Ago().ToSignature()
                                                             }
                                                         }
                                                     }
                                      },
                         Commit = commitTwoOnFeature,
                         FeatureBranch = featureBranch,
                         FindFirstCommitOnBranchFunc = () => branchingCommit.Id
                     };
        var version = finder.FindVersion();
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(3, version.Version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Version.Stability);
        Assert.AreEqual(BranchType.Feature, version.BranchType);
        Assert.AreEqual(branchingCommit.Prefix(), version.Version.Suffix, "Suffix should be the develop commit it was branched from");
        Assert.AreEqual(0, version.Version.PreReleasePartOne, "Prerelease is always 0 for feature branches");
    }

    [Test]
    public void Feature_branch_with_2_commits_but_building_an_commit()
    {

        var branchingCommit = new MockCommit
                              {
                                  CommitterEx = 3.Seconds().Ago().ToSignature(),
                              };
        var commitOneOnFeature = new MockCommit
                                 {
                                     CommitterEx = 2.Seconds().Ago().ToSignature(),
                                 };
        var commitTwoOnFeature = new MockCommit
                                 {
                                     CommitterEx = 1.Seconds().Ago().ToSignature(),
                                 };
        var featureBranch = new MockBranch("featureWithNoCommits")
                            {
                                branchingCommit,
                                commitOneOnFeature,
                                commitTwoOnFeature,
                            };
        var finder = new FeatureVersionFinder
                     {
                         Repository = new MockRepository
                                      {
                                          Branches = new MockBranchCollection
                                                     {
                                                         new MockBranch("master")
                                                         {
                                                             new MockMergeCommit
                                                             {
                                                                 MessageEx = "Merge branch 'release-0.2.0'",
                                                                 CommitterEx = 4.Seconds().Ago().ToSignature()
                                                             }
                                                         },
                                                         featureBranch,
                                                         new MockBranch("develop")
                                                         {
                                                             branchingCommit,
                                                             new MockCommit
                                                             {
                                                                 CommitterEx = 2.Seconds().Ago().ToSignature()
                                                             }
                                                         }
                                                     }
                                      },
                         Commit = commitOneOnFeature,
                         FeatureBranch = featureBranch,
                         FindFirstCommitOnBranchFunc = () => branchingCommit.Id
                     };
        var version = finder.FindVersion();
        Assert.AreEqual(0, version.Version.Major);
        Assert.AreEqual(3, version.Version.Minor, "Minor should be master.Minor+1");
        Assert.AreEqual(0, version.Version.Patch);
        Assert.AreEqual(Stability.Unstable, version.Version.Stability);
        Assert.AreEqual(BranchType.Feature, version.BranchType);
        Assert.AreEqual(branchingCommit.Prefix(), version.Version.Suffix, "Suffix should be the develop commit it was branched from");
        Assert.AreEqual(0, version.Version.PreReleasePartOne, "Prerelease is always 0 for feature branches");
    }
}