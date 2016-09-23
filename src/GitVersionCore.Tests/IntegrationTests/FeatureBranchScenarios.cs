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
            fixture.Repository.Checkout("development");

            //Create an initial feature branch
            var feature123 = fixture.Repository.CreateBranch("feature/JIRA-123");
            fixture.Repository.Checkout("feature/JIRA-123");
            fixture.Repository.MakeCommits(1);

            //Merge it
            fixture.Repository.Checkout("development");
            fixture.Repository.Merge(feature123, Generate.SignatureNow());

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
                { "unstable", new BranchConfig { Increment = IncrementStrategy.Minor, Regex = "unstable"} }
            }
        };

        using (var fixture = new EmptyRepositoryFixture())
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
            fixture.Repository.Merge(feature123, Generate.SignatureNow());

            //Create a second feature branch
            fixture.Repository.CreateBranch("feature/JIRA-124");
            fixture.Repository.Checkout("feature/JIRA-124");
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
        using (var fixture = new EmptyRepositoryFixture())
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
        using (var fixture = new EmptyRepositoryFixture())
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
        using (var fixture = new EmptyRepositoryFixture())
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
        using (var fixture = new EmptyRepositoryFixture())
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
        using (var fixture = new EmptyRepositoryFixture())
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
            fixture.Repository.Merge(fixture.Repository.Branches["develop"], Generate.SignatureNow());
            fixture.Repository.ApplyTag("v1.1.0");

            fixture.Repository.Checkout("feature/longrunning");
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
            fixture.Repository.Checkout(featureBranchName);
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
            fixture.Repository.Checkout("release/0.2.0");

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
                            IsDevelop = true,
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
                            IsDevelop = true,
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
        [TestCase(IncrementStrategy.Inherit)]
        [TestCase(IncrementStrategy.Major)]
        [TestCase(IncrementStrategy.Minor)]
        [TestCase(IncrementStrategy.None)]
        [TestCase(IncrementStrategy.Patch)]
        public void ShouldPickUpVersionFromDevelopAfterReleaseBranchCreated(IncrementStrategy strategy)
        {
            var config = new Config
            {
                Branches = new Dictionary<string, BranchConfig>
                {
                    {
                        "misnamed", new BranchConfig()
                        {
                            Increment = strategy
                        }
                    }
                }
            };

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
                fixture.AssertFullSemver(config, "1.1.0-alpha.1");

                // create a misnamed feature branch (should have no config; for testing, only Increment set) from develop and verify the version
                fixture.BranchTo("misnamed");
                fixture.AssertFullSemver(config, "1.1.0+1");
            }
        }

        [TestCase(IncrementStrategy.Inherit)]
        [TestCase(IncrementStrategy.Major)]
        [TestCase(IncrementStrategy.Minor)]
        [TestCase(IncrementStrategy.None)]
        [TestCase(IncrementStrategy.Patch)]
        public void ShouldPickUpVersionFromDevelopAfterReleaseBranchMergedBack(IncrementStrategy strategy)
        {
            var config = new Config
            {
                Branches = new Dictionary<string, BranchConfig>
                {
                    {
                        "misnamed", new BranchConfig()
                        {
                            Increment = strategy
                        }
                    }
                }
            };

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
                fixture.AssertFullSemver(config, "1.1.0-alpha.2");

                // create a misnamed feature branch (should have no config; for testing, only Increment set) from develop and verify the version
                fixture.BranchTo("misnamed");
                fixture.AssertFullSemver(config, "1.1.0+0");
            }
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public class WhenMasterMarkedAsIsDevelop
        {
            [TestCase(IncrementStrategy.Inherit)]
            [TestCase(IncrementStrategy.Major)]
            [TestCase(IncrementStrategy.Minor)]
            [TestCase(IncrementStrategy.None)]
            [TestCase(IncrementStrategy.Patch)]
            public void ShouldPickUpVersionFromMasterAfterReleaseBranchCreated(IncrementStrategy strategy)
            {
                var config = new Config
                {
                    Branches = new Dictionary<string, BranchConfig>
                    {
                        {
                            "master", new BranchConfig()
                            {
                                IsDevelop = true,
                            }
                        },
                        {
                            "misnamed", new BranchConfig()
                            {
                                Increment = strategy
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

                    // create a misnamed feature branch (should have no config; for testing, only Increment set) from master and verify the version
                    fixture.BranchTo("misnamed");
                    fixture.AssertFullSemver(config, "1.0.1+1");
                }
            }

            [TestCase(IncrementStrategy.Inherit)]
            [TestCase(IncrementStrategy.Major)]
            [TestCase(IncrementStrategy.Minor)]
            [TestCase(IncrementStrategy.None)]
            [TestCase(IncrementStrategy.Patch)]
            public void ShouldPickUpVersionFromMasterAfterReleaseBranchMergedBack(IncrementStrategy strategy)
            {
                var config = new Config
                {
                    Branches = new Dictionary<string, BranchConfig>
                    {
                        {
                            "master", new BranchConfig()
                            {
                                IsDevelop = true,
                            }
                        },
                        {
                            "misnamed", new BranchConfig()
                            {
                                Increment = strategy
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

                    // create a misnamed feature branch (should have no config; for testing, only Increment set) from master and verify the version
                    fixture.BranchTo("misnamed");
                    fixture.AssertFullSemver(config, "1.0.1+2");
                }
            }
        }
    }

    [Test]
    public void ShouldPickupConfigsFromParentBranches() {
        var config = new Config {
            Branches = new Dictionary<string, BranchConfig>
            {
                        {
                            "master", new BranchConfig()
                            {
                                Increment = IncrementStrategy.Major
                            }
                        },
                        {
                            "child_minor", new BranchConfig()
                            {
                                Increment = IncrementStrategy.Minor
                            }
                        },
                        {
                            "child_patch", new BranchConfig()
                            {
                                Increment = IncrementStrategy.Patch
                            }
                        },
                        {
                            "child_inherit", new BranchConfig()
                            {
                                Increment = IncrementStrategy.Inherit
                            }
                        }
                    }
        };

        using (var fixture = new EmptyRepositoryFixture()) {
            fixture.MakeACommit();

            // tag master => 1.0.0
            fixture.ApplyTag("1.0.0");
            fixture.MakeACommit();
            fixture.AssertFullSemver(config, "2.0.0+1");

            // branch to child_minor => same version number as master
            fixture.BranchTo("child_minor");
            fixture.MakeACommit();
            fixture.AssertFullSemver(config, "2.0.0+3");

            // tag child_minor as 3.2.0 => 3.3
            fixture.ApplyTag("3.2.0");
            fixture.MakeACommit();
            fixture.AssertFullSemver(config, "3.3.0+1");

            // branch to child_patch from child_minor => same version number as child_minor
            fixture.BranchTo("child_patch");
            fixture.MakeACommit();
            fixture.AssertFullSemver(config, "3.3.0+2");

            // branch to child_inherit from master => same version number as master
            fixture.Checkout("master");
            fixture.MakeACommit();
            fixture.BranchTo("child_inherit");
            fixture.MakeACommit();
            fixture.AssertFullSemver(config, "2.0.0+3");

            // tag child_inherit as 5.0.0 => 6.0
            fixture.ApplyTag("5.0.0");
            fixture.MakeACommit();
            fixture.AssertFullSemver(config, "6.0.0+1");
        }
    }
}