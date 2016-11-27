using System.Collections.Generic;
using GitTools.Testing;
using GitVersion;
using GitVersionCore.Tests;
using LibGit2Sharp;
using NUnit.Framework;

[TestFixture]
public class FeatureBranchScenarios
{
    [Test]
    public void ShouldInheritIncrementCorrectlyWithMultiplePossibleParentsAndWeirdlyNamedDevelopBranch()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("development");
            Commands.Checkout(fixture.Repository, "development");

            //Create an initial feature branch
            var feature123 = fixture.Repository.CreateBranch("feature/JIRA-123");
            Commands.Checkout(fixture.Repository, "feature/JIRA-123");
            fixture.Repository.MakeCommits(1);

            //Merge it
            Commands.Checkout(fixture.Repository, "development");
            fixture.Repository.Merge(feature123, Generate.SignatureNow());

            //Create a second feature branch
            fixture.Repository.CreateBranch("feature/JIRA-124");
            Commands.Checkout(fixture.Repository, "feature/JIRA-124");
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
                { "unstable", new BranchConfig { Increment = IncrementStrategy.Minor, Regex = "unstable"} }
            }
        };

        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("unstable");
            Commands.Checkout(fixture.Repository, "unstable");

            //Create an initial feature branch
            var feature123 = fixture.Repository.CreateBranch("feature/JIRA-123");
            Commands.Checkout(fixture.Repository, "feature/JIRA-123");
            fixture.Repository.MakeCommits(1);

            //Merge it
            Commands.Checkout(fixture.Repository, "unstable");
            fixture.Repository.Merge(feature123, Generate.SignatureNow());

            //Create a second feature branch
            fixture.Repository.CreateBranch("feature/JIRA-124");
            Commands.Checkout(fixture.Repository, "feature/JIRA-124");
            fixture.Repository.MakeCommits(1);

            fixture.AssertFullSemver(config, "1.1.0-JIRA-124.1+2");
        }
    }

    [Test]
    public void ShouldNotUseNumberInFeatureBranchAsPreReleaseNumberOffDevelop()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("develop");
            Commands.Checkout(fixture.Repository, "develop");
            fixture.Repository.CreateBranch("feature/JIRA-123");
            Commands.Checkout(fixture.Repository, "feature/JIRA-123");
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver("1.1.0-JIRA-123.1+5");
        }
    }

    [Test]
    public void ShouldNotUseNumberInFeatureBranchAsPreReleaseNumberOffMaster()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("feature/JIRA-123");
            Commands.Checkout(fixture.Repository, "feature/JIRA-123");
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver("1.0.1-JIRA-123.1+5");
        }
    }

    [Test]
    public void TestFeatureBranch()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("feature-test");
            Commands.Checkout(fixture.Repository, "feature-test");
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver("1.0.1-test.1+5");
        }
    }

    [Test]
    public void TestFeaturesBranch()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            fixture.Repository.CreateBranch("features/test");
            Commands.Checkout(fixture.Repository, "features/test");
            fixture.Repository.MakeCommits(5);

            fixture.AssertFullSemver("1.0.1-test.1+5");
        }
    }

    [Test]
    public void WhenTwoFeatureBranchPointToTheSameCommit()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("develop");
            Commands.Checkout(fixture.Repository, "develop");
            fixture.Repository.CreateBranch("feature/feature1");
            Commands.Checkout(fixture.Repository, "feature/feature1");
            fixture.Repository.MakeACommit();
            fixture.Repository.CreateBranch("feature/feature2");
            Commands.Checkout(fixture.Repository, "feature/feature2");

            fixture.AssertFullSemver("0.1.0-feature2.1+1");
        }
    }

    [Test]
    public void ShouldBePossibleToMergeDevelopForALongRunningBranchWhereDevelopAndMasterAreEqual()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("v1.0.0");

            fixture.Repository.CreateBranch("develop");
            Commands.Checkout(fixture.Repository, "develop");

            fixture.Repository.CreateBranch("feature/longrunning");
            Commands.Checkout(fixture.Repository, "feature/longrunning");
            fixture.Repository.MakeACommit();

            Commands.Checkout(fixture.Repository, "develop");
            fixture.Repository.MakeACommit();

            Commands.Checkout(fixture.Repository, "master");
            fixture.Repository.Merge(fixture.Repository.Branches["develop"], Generate.SignatureNow());
            fixture.Repository.ApplyTag("v1.1.0");

            Commands.Checkout(fixture.Repository, "feature/longrunning");
            fixture.Repository.Merge(fixture.Repository.Branches["develop"], Generate.SignatureNow());

            var configuration = new Config { VersioningMode = VersioningMode.ContinuousDeployment };
            fixture.AssertFullSemver(configuration, "1.2.0-longrunning.2");
        }
    }

    [TestCase("alpha", "JIRA-123", "alpha")]
    [TestCase("useBranchName", "JIRA-123", "JIRA-123")]
    [TestCase("alpha.{BranchName}", "JIRA-123", "alpha.JIRA-123")]
    public void ShouldUseConfiguredTag(string tag, string featureName, string preReleaseTagName)
    {
        var config = new Config
        {
            Branches =
            {
                { "feature", new BranchConfig { Tag = tag } }
            }
        };

        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.Repository.MakeATaggedCommit("1.0.0");
            var featureBranchName = string.Format("feature/{0}", featureName);
            fixture.Repository.CreateBranch(featureBranchName);
            Commands.Checkout(fixture.Repository, featureBranchName);
            fixture.Repository.MakeCommits(5);

            var expectedFullSemVer = string.Format("1.0.1-{0}.1+5", preReleaseTagName);
            fixture.AssertFullSemver(config, expectedFullSemVer);
        }
    }

    [Test]
    public void BranchCreatedAfterFinishReleaseShouldInheritAndIncrementFromLastMasterCommitTag()
    {
        using (var fixture = new BaseGitFlowRepositoryFixture("0.1.0"))
        {
            //validate current version
            fixture.AssertFullSemver("0.2.0-alpha.1");
            fixture.Repository.CreateBranch("release/0.2.0");
            Commands.Checkout(fixture.Repository, "release/0.2.0");

            //validate release version
            fixture.AssertFullSemver("0.2.0-beta.1+0");

            fixture.Checkout("master");
            fixture.Repository.MergeNoFF("release/0.2.0");
            fixture.Repository.ApplyTag("0.2.0");

            //validate master branch version
            fixture.AssertFullSemver("0.2.0");

            fixture.Checkout("develop");
            fixture.Repository.MergeNoFF("release/0.2.0");
            fixture.Repository.Branches.Remove("release/2.0.0");

            fixture.Repository.MakeACommit();

            //validate develop branch version after merging release 0.2.0 to master and develop (finish release)
            fixture.AssertFullSemver("0.3.0-alpha.1");

            //create a feature branch from develop
            fixture.BranchTo("feature/TEST-1");
            fixture.Repository.MakeACommit();

            //I'm not entirely sure what the + value should be but I know the semvar major/minor/patch should be 0.3.0
            fixture.AssertFullSemver("0.3.0-TEST-1.1+2");
        }
    }

    [Test]
    public void ShouldPickUpVersionFromDevelopAfterReleaseBranchCreated()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            // Create develop and release branches
            fixture.MakeACommit();
            fixture.BranchTo("develop");
            fixture.MakeACommit();
            fixture.BranchTo("release/1.0");
            fixture.MakeACommit();
            fixture.Checkout("develop");
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.1.0-alpha.1");

            // create a feature branch from develop and verify the version
            fixture.BranchTo("feature/test");
            fixture.AssertFullSemver("1.1.0-test.1+1");
        }
    }

    [Test]
    public void ShouldPickUpVersionFromDevelopAfterReleaseBranchMergedBack()
    {
        using (var fixture = new EmptyRepositoryFixture())
        {
            // Create develop and release branches
            fixture.MakeACommit();
            fixture.BranchTo("develop");
            fixture.MakeACommit();
            fixture.BranchTo("release/1.0");
            fixture.MakeACommit();

            // merge release into develop
            fixture.Checkout("develop");
            fixture.MergeNoFF("release/1.0");
            fixture.AssertFullSemver("1.1.0-alpha.2");

            // create a feature branch from develop and verify the version
            fixture.BranchTo("feature/test");
            fixture.AssertFullSemver("1.1.0-test.1+2");
        }
    }

    public class WhenMasterMarkedAsIsDevelop
    {
        [Test]
        public void ShouldPickUpVersionFromMasterAfterReleaseBranchCreated()
        {
            var config = new Config
            {
                Branches = new Dictionary<string, BranchConfig>
                {
                    {
                        "master", new BranchConfig()
                        {
                            TracksReleaseBranches = true,
                            Regex = "master"
                        }
                    }
                }
            };

            using (var fixture = new EmptyRepositoryFixture())
            {
                // Create release branch
                fixture.MakeACommit();
                fixture.BranchTo("release/1.0");
                fixture.MakeACommit();
                fixture.Checkout("master");
                fixture.MakeACommit();
                fixture.AssertFullSemver(config, "1.0.1+1");

                // create a feature branch from master and verify the version
                fixture.BranchTo("feature/test");
                fixture.AssertFullSemver(config, "1.0.1-test.1+1");
            }
        }

        [Test]
        public void ShouldPickUpVersionFromMasterAfterReleaseBranchMergedBack()
        {
            var config = new Config
            {
                Branches = new Dictionary<string, BranchConfig>
                {
                    {
                        "master", new BranchConfig()
                        {
                            TracksReleaseBranches = true,
                            Regex = "master"
                        }
                    }
                }
            };

            using (var fixture = new EmptyRepositoryFixture())
            {
                // Create release branch
                fixture.MakeACommit();
                fixture.BranchTo("release/1.0");
                fixture.MakeACommit();

                // merge release into master
                fixture.Checkout("master");
                fixture.MergeNoFF("release/1.0");
                fixture.AssertFullSemver(config, "1.0.1+2");

                // create a feature branch from master and verify the version
                fixture.BranchTo("feature/test");
                fixture.AssertFullSemver(config, "1.0.1-test.1+2");
            }
        }
    }

    public class WhenFeatureBranchHasNoConfig
    {
        [Test]
        public void ShouldPickUpVersionFromMasterAfterReleaseBranchCreated()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                // Create develop and release branches
                fixture.MakeACommit();
                fixture.BranchTo("develop");
                fixture.MakeACommit();
                fixture.BranchTo("release/1.0");
                fixture.MakeACommit();
                fixture.Checkout("develop");
                fixture.MakeACommit();
                fixture.AssertFullSemver("1.1.0-alpha.1");

                // create a misnamed feature branch (i.e. it uses the default config) from develop and verify the version
                fixture.BranchTo("misnamed");
                fixture.AssertFullSemver("1.1.0-misnamed.1+1");
            }
        }

        [Test]
        public void ShouldPickUpVersionFromDevelopAfterReleaseBranchMergedBack()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                // Create develop and release branches
                fixture.MakeACommit();
                fixture.BranchTo("develop");
                fixture.MakeACommit();
                fixture.BranchTo("release/1.0");
                fixture.MakeACommit();

                // merge release into develop
                fixture.Checkout("develop");
                fixture.MergeNoFF("release/1.0");
                fixture.AssertFullSemver("1.1.0-alpha.2");

                // create a misnamed feature branch (i.e. it uses the default config) from develop and verify the version
                fixture.BranchTo("misnamed");
                fixture.AssertFullSemver("1.1.0-misnamed.1+2");
            }
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public class WhenMasterMarkedAsIsDevelop
        {
            [Test]
            public void ShouldPickUpVersionFromMasterAfterReleaseBranchCreated()
            {
                var config = new Config
                {
                    Branches = new Dictionary<string, BranchConfig>
                    {
                        {
                            "master", new BranchConfig()
                            {
                                TracksReleaseBranches = true,
                                Regex = "master"
                            }
                        }
                    }
                };

                using (var fixture = new EmptyRepositoryFixture())
                {
                    // Create release branch
                    fixture.MakeACommit();
                    fixture.BranchTo("release/1.0");
                    fixture.MakeACommit();
                    fixture.Checkout("master");
                    fixture.MakeACommit();
                    fixture.AssertFullSemver(config, "1.0.1+1");

                    // create a misnamed feature branch (i.e. it uses the default config) from master and verify the version
                    fixture.BranchTo("misnamed");
                    fixture.AssertFullSemver(config, "1.0.1-misnamed.1+1");
                }
            }

            [Test]
            public void ShouldPickUpVersionFromMasterAfterReleaseBranchMergedBack()
            {
                var config = new Config
                {
                    Branches = new Dictionary<string, BranchConfig>
                    {
                        {
                            "master", new BranchConfig()
                            {
                                TracksReleaseBranches = true,
                                Regex = "master"
                            }
                        }
                    }
                };

                using (var fixture = new EmptyRepositoryFixture())
                {
                    // Create release branch
                    fixture.MakeACommit();
                    fixture.BranchTo("release/1.0");
                    fixture.MakeACommit();

                    // merge release into master
                    fixture.Checkout("master");
                    fixture.MergeNoFF("release/1.0");
                    fixture.AssertFullSemver(config, "1.0.1+2");

                    // create a misnamed feature branch (i.e. it uses the default config) from master and verify the version
                    fixture.BranchTo("misnamed");
                    fixture.AssertFullSemver(config, "1.0.1-misnamed.1+2");
                }
            }
        }
    }

    [Test]
    public void PickUpVersionFromMasterMarkedWithIsTracksReleaseBranches()
    {
        var config = new Config
        {
            VersioningMode = VersioningMode.ContinuousDelivery,
            Branches = new Dictionary<string, BranchConfig>
                {
                    {
                        "master", new BranchConfig()
                        {
                            Tag = "pre",
                            TracksReleaseBranches = true,
                        }
                    },
                    {
                        "release", new BranchConfig()
                        {
                            IsReleaseBranch = true,
                            Tag = "rc",
                        }
                    }
                }
        };

        using (var fixture = new EmptyRepositoryFixture())
        {
            fixture.MakeACommit();

            // create a release branch and tag a release
            fixture.BranchTo("release/0.10.0");
            fixture.MakeACommit();
            fixture.MakeACommit();
            fixture.AssertFullSemver(config, "0.10.0-rc.1+2");

            // switch to master and verify the version
            fixture.Checkout("master");
            fixture.MakeACommit();
            fixture.AssertFullSemver(config, "0.10.1-pre.1+1");

            // create a feature branch from master and verify the version
            fixture.BranchTo("MyFeatureD");
            fixture.AssertFullSemver(config, "0.10.1-MyFeatureD.1+1");
        }
    }
}
